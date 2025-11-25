import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import Navigation from "@/components/Navigation";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
});

export const metadata: Metadata = {
  title: "SkyHigh Manga - Đọc truyện tranh online",
  description: "Đọc truyện tranh online miễn phí, cập nhật nhanh nhất",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi" className="scroll-smooth">
      <body className={`${inter.variable} font-sans antialiased bg-gray-50 dark:bg-gray-900`}>
        <Navigation />
        <main className="min-h-screen">
          {children}
        </main>
        <footer className="bg-white dark:bg-gray-800 border-t border-gray-200 dark:border-gray-700 mt-12">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
            <p className="text-center text-gray-600 dark:text-gray-400">
              © 2025 SkyHigh Manga. All rights reserved.
            </p>
          </div>
        </footer>
      </body>
    </html>
  );
}
