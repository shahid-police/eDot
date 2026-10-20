/** @type {import('next').NextConfig} */
const nextConfig = {
  images: {
    unoptimized: true,
  },
  // Home2 is the only homepage the project keeps (home-1/home-3..6 were removed).
  // Temporary (307) rather than permanent so this is easy to change later and isn't
  // aggressively cached by browsers while the project is still under active development.
  async redirects() {
    return [
      {
        source: "/",
        destination: "/home-2",
        permanent: false,
      },
    ];
  },
};

export default nextConfig;
