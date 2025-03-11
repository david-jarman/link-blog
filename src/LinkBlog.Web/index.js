const trix = require('trix');

document.addEventListener("trix-before-initialize", () => {
    // Change Trix.config if you need
    console.log("trix-before-initialize");
    console.log(Trix);
    console.log(Trix.config);
})