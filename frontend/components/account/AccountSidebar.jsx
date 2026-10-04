"use client";
import { accountLinks } from "@/data/menu";
import Link from "next/link";
import { usePathname } from "next/navigation";
import React from "react";
import { logout } from "@/lib/api/auth";

export default function AccountSidebar() {
  const pathname = usePathname();

  const signOut = async (event) => {
    event.preventDefault();
    await logout();
  };

  return (
    <>
      {accountLinks.map((link, index) => {
        const className = `link-account${
          link.href.split("/")[1] == pathname.split("/")[1] ? " active" : ""
        }`;
        if (link.text === "Log Out") {
          return (
            <li key={index}>
              <a href="#" className={className} onClick={signOut}>
                <span className="text">{link.text}</span>
              </a>
            </li>
          );
        }
        return (
          <li key={index}>
            <Link href={link.href} className={className}>
              <span className="text">{link.text}</span>
            </Link>
          </li>
        );
      })}
    </>
  );
}
