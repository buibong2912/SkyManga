import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 'cdn.truyennganhay.net',
        pathname: '/**',
      },
      {
        protocol: 'http',
        hostname: 'cdn.truyennganhay.net',
        pathname: '/**',
      },
      // Thêm các CDN khác nếu cần
      {
        protocol: 'https',
        hostname: '**.nettruyen.com',
        pathname: '/**',
      },
      {
        protocol: 'http',
        hostname: '**.nettruyen.com',
        pathname: '/**',
      },
      // Cho phép tất cả các domain (chỉ trong development)
      ...(process.env.NODE_ENV === 'development' ? [
        {
          protocol: 'https',
          hostname: '**',
        },
        {
          protocol: 'http',
          hostname: '**',
        },
      ] : []),
    ],
    // Cho phép unoptimized images nếu cần (không khuyến khích cho production)
    unoptimized: false,
  },
};

export default nextConfig;
