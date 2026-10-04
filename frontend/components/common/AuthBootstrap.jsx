"use client";
import { useEffect } from "react";
import { restoreSession } from "@/lib/api/auth";

export default function AuthBootstrap() {
  useEffect(() => {
    restoreSession();
  }, []);
  return null;
}
