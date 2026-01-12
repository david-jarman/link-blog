#!/usr/bin/env python3
"""
Simple HTTP proxy that adds Proxy-Authorization header to upstream proxy.
This works around .NET's inability to send proxy credentials properly.
"""

import socket
import threading
import base64
import os
import urllib.parse

def parse_proxy_url(proxy_url):
    """Parse proxy URL to extract host, port, username, and password."""
    parsed = urllib.parse.urlparse(proxy_url)
    username = parsed.username or ""
    password = parsed.password or ""
    host = parsed.hostname
    port = parsed.port or 8080
    return host, port, username, password

def forward_data(source, destination):
    """Forward data from source to destination socket."""
    try:
        while True:
            data = source.recv(4096)
            if not data:
                break
            destination.sendall(data)
    except:
        pass

def handle_client(client_socket, upstream_host, upstream_port, auth_header):
    """Handle a client connection by forwarding to upstream proxy with auth."""
    upstream = None
    try:
        # Read the first line to determine if it's a CONNECT request
        request = b''
        while b'\r\n\r\n' not in request:
            chunk = client_socket.recv(4096)
            if not chunk:
                return
            request += chunk
            if len(request) > 100000:  # Prevent reading too much
                return

        # Parse the request
        request_str = request.decode('latin-1', errors='ignore')
        lines = request_str.split('\r\n')
        request_line = lines[0] if lines else ""

        # Check if this is a CONNECT request (for HTTPS)
        is_connect = request_line.startswith('CONNECT ')

        # Add Proxy-Authorization header if not present
        has_proxy_auth = any(line.startswith('Proxy-Authorization:') for line in lines)

        if not has_proxy_auth and auth_header:
            # Insert after the first line (request line)
            lines.insert(1, f'Proxy-Authorization: Basic {auth_header}')
            request = '\r\n'.join(lines).encode('latin-1')

        # Connect to upstream proxy
        upstream = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        upstream.settimeout(30)
        upstream.connect((upstream_host, upstream_port))

        # Send modified request to upstream
        upstream.sendall(request)

        if is_connect:
            # For CONNECT, wait for 200 response from upstream, then start bidirectional forwarding
            response = b''
            while b'\r\n\r\n' not in response:
                chunk = upstream.recv(4096)
                if not chunk:
                    break
                response += chunk

            # Send response to client
            client_socket.sendall(response)

            # If connection established, start bidirectional forwarding
            if b'200' in response[:50]:  # Check if 200 OK in status line
                # Start bidirectional forwarding
                client_thread = threading.Thread(target=forward_data, args=(client_socket, upstream))
                upstream_thread = threading.Thread(target=forward_data, args=(upstream, client_socket))

                client_thread.daemon = True
                upstream_thread.daemon = True

                client_thread.start()
                upstream_thread.start()

                # Wait for both threads
                client_thread.join()
                upstream_thread.join()
        else:
            # Regular HTTP proxy request
            while True:
                data = upstream.recv(4096)
                if not data:
                    break
                client_socket.sendall(data)

    except Exception as e:
        print(f"Error handling client: {e}")
        import traceback
        traceback.print_exc()
    finally:
        try:
            if upstream:
                upstream.close()
        except:
            pass
        try:
            client_socket.close()
        except:
            pass

def main():
    # Get upstream proxy from HTTP_PROXY environment variable
    http_proxy = os.environ.get('HTTP_PROXY') or os.environ.get('http_proxy')
    if not http_proxy:
        print("Error: HTTP_PROXY environment variable not set")
        return 1

    # Parse upstream proxy
    upstream_host, upstream_port, username, password = parse_proxy_url(http_proxy)

    # Create auth header
    auth_header = None
    if username and password:
        credentials = f"{username}:{password}"
        auth_header = base64.b64encode(credentials.encode()).decode()
        print(f"Upstream proxy: {upstream_host}:{upstream_port}")
        print(f"Auth: {username[:30]}... / {len(password)} char password")
    else:
        print(f"Upstream proxy: {upstream_host}:{upstream_port} (no auth)")

    # Start local proxy server
    listen_host = '127.0.0.1'
    listen_port = 3128

    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((listen_host, listen_port))
    server.listen(5)

    print(f"Local proxy listening on {listen_host}:{listen_port}")
    print(f"Set HTTP_PROXY=http://{listen_host}:{listen_port} to use this proxy")
    print("Press Ctrl+C to stop")

    try:
        while True:
            client, addr = server.accept()
            thread = threading.Thread(
                target=handle_client,
                args=(client, upstream_host, upstream_port, auth_header)
            )
            thread.daemon = True
            thread.start()
    except KeyboardInterrupt:
        print("\nShutting down...")
    finally:
        server.close()

if __name__ == '__main__':
    exit(main() or 0)
