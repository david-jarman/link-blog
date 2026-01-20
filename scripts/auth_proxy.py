#!/usr/bin/env python3
"""
Local proxy that forwards requests to an authenticated upstream proxy.

This script solves the issue where some tools (like NuGet) don't properly
pass credentials from HTTP_PROXY environment variable. It runs a local
proxy without authentication that adds the credentials when forwarding
to the upstream proxy.

Usage:
    export HTTP_PROXY="http://user:pass@proxy.example.com:8080"
    python3 auth_proxy.py
    # Then set HTTP_PROXY=http://localhost:3128 for your tools
"""

import base64
import os
import select
import socket
import sys
import threading
from http.server import HTTPServer, BaseHTTPRequestHandler
from urllib.parse import urlparse

LOCAL_PORT = 3128
BUFFER_SIZE = 65536


def parse_proxy_url(proxy_url: str) -> tuple[str, int, str | None]:
    """Parse proxy URL and extract host, port, and auth header."""
    parsed = urlparse(proxy_url)

    host = parsed.hostname
    port = parsed.port or 8080

    auth_header = None
    if parsed.username:
        credentials = f"{parsed.username}:{parsed.password or ''}"
        encoded = base64.b64encode(credentials.encode()).decode()
        auth_header = f"Basic {encoded}"

    return host, port, auth_header


def get_upstream_proxy() -> tuple[str, int, str | None]:
    """Get upstream proxy details from environment."""
    proxy_url = os.environ.get("UPSTREAM_HTTP_PROXY") or os.environ.get("HTTP_PROXY")

    if not proxy_url:
        print("Error: No upstream proxy configured.", file=sys.stderr)
        print("Set UPSTREAM_HTTP_PROXY or HTTP_PROXY environment variable.", file=sys.stderr)
        sys.exit(1)

    return parse_proxy_url(proxy_url)


class ProxyHandler(BaseHTTPRequestHandler):
    """HTTP request handler that forwards to upstream proxy with auth."""

    # Class-level upstream proxy config
    upstream_host: str
    upstream_port: int
    upstream_auth: str | None

    def log_message(self, format: str, *args) -> None:
        """Suppress default HTTP request logging."""
        pass

    def do_CONNECT(self) -> None:
        """Handle HTTPS CONNECT tunneling."""
        upstream = None
        try:
            # Connect to upstream proxy
            upstream = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            upstream.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
            upstream.connect((self.upstream_host, self.upstream_port))

            # Send CONNECT request to upstream proxy with auth
            connect_request = f"CONNECT {self.path} HTTP/1.1\r\n"
            connect_request += f"Host: {self.path}\r\n"
            if self.upstream_auth:
                connect_request += f"Proxy-Authorization: {self.upstream_auth}\r\n"
            connect_request += "\r\n"

            upstream.sendall(connect_request.encode())

            # Read response from upstream proxy
            response = b""
            while b"\r\n\r\n" not in response:
                chunk = upstream.recv(BUFFER_SIZE)
                if not chunk:
                    self.send_error(502, "Upstream proxy closed connection")
                    return
                response += chunk

            # Check if upstream proxy accepted the connection
            header_end = response.find(b"\r\n\r\n")
            status_line = response.split(b"\r\n")[0].decode()
            if "200" not in status_line:
                self.send_error(502, f"Upstream proxy error: {status_line}")
                return

            # Check for any data after the headers (shouldn't happen, but handle it)
            extra_data = response[header_end + 4:] if header_end + 4 < len(response) else b""

            # Tell client the tunnel is established
            self.send_response(200, "Connection Established")
            self.end_headers()

            # Flush the response to client
            self.wfile.flush()

            # Get raw client socket
            client_socket = self.connection
            client_socket.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)

            # If there was extra data from upstream, send it to client
            if extra_data:
                client_socket.sendall(extra_data)

            # Tunnel data between client and upstream
            self._tunnel(client_socket, upstream)
            upstream = None  # Prevent double-close

        except Exception as e:
            self.log_message(f"CONNECT error to {self.path}: {e}")
            try:
                self.send_error(502, str(e))
            except:
                pass
        finally:
            if upstream:
                upstream.close()

    def _tunnel(self, client: socket.socket, upstream: socket.socket) -> None:
        """Bidirectional tunnel between client and upstream."""
        # Keep sockets blocking - select() handles multiplexing, blocking ensures
        # sendall() works correctly (non-blocking sendall can lose data)
        client.setblocking(True)
        upstream.setblocking(True)

        try:
            while True:
                # Use longer timeout - TLS handshakes and idle connections need time
                readable, _, exceptional = select.select(
                    [client, upstream], [], [client, upstream], 300
                )

                if exceptional:
                    break

                if not readable:
                    # Timeout after 5 minutes of no activity
                    break

                for sock in readable:
                    other = upstream if sock is client else client
                    try:
                        data = sock.recv(BUFFER_SIZE)
                        if not data:
                            # Connection closed cleanly
                            return
                        other.sendall(data)
                    except (ConnectionResetError, BrokenPipeError, OSError):
                        # Connection error
                        return
        finally:
            try:
                upstream.close()
            except:
                pass

    def do_GET(self) -> None:
        self._forward_request()

    def do_POST(self) -> None:
        self._forward_request()

    def do_PUT(self) -> None:
        self._forward_request()

    def do_DELETE(self) -> None:
        self._forward_request()

    def do_HEAD(self) -> None:
        self._forward_request()

    def do_OPTIONS(self) -> None:
        self._forward_request()

    def do_PATCH(self) -> None:
        self._forward_request()

    def _forward_request(self) -> None:
        """Forward HTTP request through upstream proxy."""
        try:
            # Connect to upstream proxy
            upstream = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            upstream.connect((self.upstream_host, self.upstream_port))

            # Build request to send to upstream proxy
            # For HTTP proxying, we send the full URL
            request = f"{self.command} {self.path} {self.request_version}\r\n"

            # Forward headers, adding proxy auth
            for header, value in self.headers.items():
                if header.lower() not in ("proxy-authorization",):
                    request += f"{header}: {value}\r\n"

            if self.upstream_auth:
                request += f"Proxy-Authorization: {self.upstream_auth}\r\n"

            request += "\r\n"
            upstream.sendall(request.encode())

            # Forward request body if present
            content_length = self.headers.get("Content-Length")
            if content_length:
                body = self.rfile.read(int(content_length))
                upstream.sendall(body)

            # Read and forward response
            self._forward_response(upstream)

        except Exception as e:
            self.log_message(f"Forward error: {e}")
            try:
                self.send_error(502, str(e))
            except:
                pass

    def _forward_response(self, upstream: socket.socket) -> None:
        """Read response from upstream and forward to client."""
        try:
            # Read headers
            response = b""
            while b"\r\n\r\n" not in response:
                chunk = upstream.recv(BUFFER_SIZE)
                if not chunk:
                    break
                response += chunk

            # Split headers and any body data received
            header_end = response.find(b"\r\n\r\n")
            headers = response[:header_end]
            body_start = response[header_end + 4:]

            # Send headers to client
            self.wfile.write(headers + b"\r\n\r\n")

            # Send any body data already received
            if body_start:
                self.wfile.write(body_start)

            # Check for chunked or content-length
            headers_lower = headers.lower()

            if b"transfer-encoding: chunked" in headers_lower:
                # Stream chunked response
                self._stream_chunked(upstream, body_start)
            elif b"content-length:" in headers_lower:
                # Stream fixed-length response
                for line in headers.split(b"\r\n"):
                    if line.lower().startswith(b"content-length:"):
                        content_length = int(line.split(b":")[1].strip())
                        remaining = content_length - len(body_start)
                        while remaining > 0:
                            chunk = upstream.recv(min(BUFFER_SIZE, remaining))
                            if not chunk:
                                break
                            self.wfile.write(chunk)
                            remaining -= len(chunk)
                        break
            else:
                # Stream until connection closes
                while True:
                    chunk = upstream.recv(BUFFER_SIZE)
                    if not chunk:
                        break
                    self.wfile.write(chunk)
        except (BrokenPipeError, ConnectionResetError):
            # Client disconnected, nothing to do
            pass
        finally:
            upstream.close()

    def _stream_chunked(self, upstream: socket.socket, initial: bytes) -> None:
        """Stream chunked transfer encoding response."""
        buffer = initial

        while True:
            # Find chunk size line
            while b"\r\n" not in buffer:
                chunk = upstream.recv(BUFFER_SIZE)
                if not chunk:
                    return
                buffer += chunk

            line_end = buffer.find(b"\r\n")
            size_line = buffer[:line_end]
            buffer = buffer[line_end + 2:]

            # Parse chunk size (ignore extensions)
            try:
                chunk_size = int(size_line.split(b";")[0], 16)
            except ValueError:
                return

            # Write size line
            self.wfile.write(size_line + b"\r\n")

            if chunk_size == 0:
                # Final chunk - read trailing headers
                self.wfile.write(b"\r\n")
                return

            # Read chunk data + CRLF
            bytes_needed = chunk_size + 2 - len(buffer)
            while bytes_needed > 0:
                chunk = upstream.recv(min(BUFFER_SIZE, bytes_needed))
                if not chunk:
                    return
                buffer += chunk
                bytes_needed -= len(chunk)

            # Write chunk data + CRLF
            self.wfile.write(buffer[:chunk_size + 2])
            buffer = buffer[chunk_size + 2:]


class ThreadedHTTPServer(HTTPServer):
    """HTTP server that handles each request in a new thread."""

    def process_request(self, request, client_address):
        thread = threading.Thread(target=self._handle_request, args=(request, client_address))
        thread.daemon = True
        thread.start()

    def _handle_request(self, request, client_address):
        try:
            self.finish_request(request, client_address)
        except Exception:
            self.handle_error(request, client_address)
        finally:
            self.shutdown_request(request)


def main():
    # Get upstream proxy config
    upstream_host, upstream_port, upstream_auth = get_upstream_proxy()

    # Configure handler with upstream proxy details
    ProxyHandler.upstream_host = upstream_host
    ProxyHandler.upstream_port = upstream_port
    ProxyHandler.upstream_auth = upstream_auth

    # Start server
    server = ThreadedHTTPServer(("127.0.0.1", LOCAL_PORT), ProxyHandler)

    print(f"Local proxy listening on http://127.0.0.1:{LOCAL_PORT}", file=sys.stderr)
    print(f"Forwarding to upstream proxy at {upstream_host}:{upstream_port}", file=sys.stderr)
    print(f"Authentication: {'enabled' if upstream_auth else 'disabled'}", file=sys.stderr)

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nShutting down...", file=sys.stderr)
        server.shutdown()


if __name__ == "__main__":
    main()
