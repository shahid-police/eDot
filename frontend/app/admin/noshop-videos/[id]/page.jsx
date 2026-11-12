"use client";

import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import NoShopVideoForm, { fromApi } from "@/components/admin/NoShopVideoForm";
import { adminRequest } from "@/lib/admin/api";

export default function EditNoShopVideoPage() {
  const { id } = useParams();
  const [video, setVideo] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    adminRequest(`/noshop/videos/${id}`)
      .then((data) => active && setVideo(data))
      .catch((err) => active && setError(err.message));
    return () => { active = false; };
  }, [id]);

  if (error) return <div className="alert alert-danger">{error}</div>;
  if (!video) return <p>Loading...</p>;

  return <NoShopVideoForm key={video.id} videoId={video.id} initial={fromApi(video)} />;
}
