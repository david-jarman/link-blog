// webpack.config.js
const path = require('path');

module.exports = {
  // Specify the entry point
  entry: './index.js',

  // Define the output configuration
  output: {
    // Name of the bundled file
    filename: 'trix.js',

    // Absolute path to the output directory (wwwroot/js)
    path: path.resolve(__dirname, 'wwwroot/js')
  },

  // Set the mode to development or production
  mode: 'production',

  // Optional: Configure loaders, plugins, etc. as needed
};