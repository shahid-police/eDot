/** @type {import('next').NextConfig} */
const nextConfig = {
  images: {
    unoptimized: true,
  },
  // Home2 is the only homepage the project keeps (home-1/home-3..6 were removed).
  // A rewrite (not a redirect) so "/" serves home-2's content directly without the browser's
  // address bar changing to /home-2 - visiting / stays /.
  async rewrites() {
    return [
      {
        source: "/",
        destination: "/home-2",
      },
    ];
  },
};

export default nextConfig;
