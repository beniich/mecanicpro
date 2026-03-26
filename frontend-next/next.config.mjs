/** @type {import('next').NextConfig} */
const config = {
  reactStrictMode: true,
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: 'http://localhost:5000/api/:path*', // Route all API calls to Gateway 
      },
      {
        source: '/hubs/:path*',
        destination: 'http://localhost:5000/hubs/:path*', // Route SignalR calls to Gateway 
      },
    ]
  },
}
export default config
