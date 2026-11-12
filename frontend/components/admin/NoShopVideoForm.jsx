"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { adminRequest, adminUpload } from "@/lib/admin/api";

const EMPTY_VIDEO = {
  title: "",
  slug: "",
  description: "",
  youTubeUrl: "",
  categoryId: "",
  videoType: "Entertainment",
  isFeatured: false,
  displayOrder: 0,
  status: "Draft",
  publishedAt: "",
  seoTitle: "",
  seoDescription: "",
  thumbnailUrl: null,
};

const VIDEO_TYPES = ["Entertainment", "Promotion", "BrandStory", "Advertisement"];
const STATUSES = ["Draft", "Published", "Unpublished"];

// Accepted by AdminMediaService.UploadNoShopVideoThumbnailAsync.
const IMAGE_ACCEPT = "image/jpeg,image/png,image/webp";

function slugify(value) {
  return value.toLowerCase().trim().replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "");
}

// Client-side preview only - the server (YouTubeUrlParser) is the real validation. Mirrors the
// same URL shapes it accepts (watch, youtu.be, Shorts, embed, or a bare ID).
function previewYouTubeId(url) {
  const trimmed = (url ?? "").trim();
  if (/^[a-zA-Z0-9_-]{11}$/.test(trimmed)) return trimmed;
  try {
    const u = new URL(trimmed);
    if (u.hostname.endsWith("youtu.be")) {
      const id = u.pathname.replace(/^\/+/, "");
      if (/^[a-zA-Z0-9_-]{11}$/.test(id)) return id;
    } else if (u.hostname.endsWith("youtube.com") || u.hostname.endsWith("youtube-nocookie.com")) {
      const v = u.searchParams.get("v");
      if (v && /^[a-zA-Z0-9_-]{11}$/.test(v)) return v;
      const segments = u.pathname.replace(/^\/+/, "").split("/");
      if (segments.length >= 2 && ["shorts", "embed", "live"].includes(segments[0]) && /^[a-zA-Z0-9_-]{11}$/.test(segments[1])) {
        return segments[1];
      }
    }
  } catch {
    return null;
  }
  return null;
}

function toDatetimeLocal(iso) {
  if (!iso) return "";
  const d = new Date(iso);
  const pad = (n) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function fromDatetimeLocal(value) {
  return value ? new Date(value).toISOString() : null;
}

export function fromApi(video) {
  return {
    ...EMPTY_VIDEO,
    title: video.title,
    slug: video.slug,
    description: video.description ?? "",
    youTubeUrl: video.youTubeUrl,
    categoryId: String(video.categoryId),
    videoType: video.videoType,
    isFeatured: video.isFeatured,
    displayOrder: video.displayOrder,
    status: video.status,
    publishedAt: toDatetimeLocal(video.publishedAt),
    seoTitle: video.seoTitle ?? "",
    seoDescription: video.seoDescription ?? "",
    thumbnailUrl: video.thumbnailUrl,
  };
}

function toPayload(form) {
  return {
    title: form.title.trim(),
    slug: form.slug.trim(),
    description: form.description || null,
    youTubeUrl: form.youTubeUrl.trim(),
    categoryId: Number(form.categoryId),
    videoType: form.videoType,
    isFeatured: form.isFeatured,
    displayOrder: Number(form.displayOrder) || 0,
    status: form.status,
    publishedAt: fromDatetimeLocal(form.publishedAt),
    seoTitle: form.seoTitle || null,
    seoDescription: form.seoDescription || null,
  };
}

export default function NoShopVideoForm({ videoId = null, initial = null }) {
  const router = useRouter();
  const isNew = videoId === null;
  const [form, setForm] = useState(initial ?? EMPTY_VIDEO);
  const [categories, setCategories] = useState([]);
  const [slugTouched, setSlugTouched] = useState(!isNew);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [thumbnailError, setThumbnailError] = useState("");
  const [thumbnailBusy, setThumbnailBusy] = useState(false);

  useEffect(() => {
    adminRequest("/noshop/categories").then(setCategories).catch((err) => setError(err.message));
  }, []);

  function update(field, value) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function updateTitle(value) {
    setForm((current) => ({
      ...current,
      title: value,
      slug: slugTouched ? current.slug : slugify(value),
    }));
  }

  async function uploadThumbnail(file) {
    setThumbnailError("");
    setThumbnailBusy(true);
    try {
      const data = new FormData();
      data.append("file", file);
      const result = await adminUpload(`/noshop/videos/${videoId}/thumbnail`, data);
      update("thumbnailUrl", result.url);
    } catch (err) {
      setThumbnailError(err.message);
    } finally {
      setThumbnailBusy(false);
    }
  }

  async function removeThumbnail() {
    setThumbnailError("");
    try {
      await adminRequest(`/noshop/videos/${videoId}/thumbnail`, { method: "DELETE" });
      update("thumbnailUrl", previewThumbnailFromYouTube ?? null);
    } catch (err) {
      setThumbnailError(err.message);
    }
  }

  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    setMessage("");
    try {
      const payload = toPayload(form);
      if (isNew) {
        const created = await adminRequest("/noshop/videos", { method: "POST", body: payload });
        router.replace(`/admin/noshop-videos/${created.id}`);
        return;
      }
      const saved = await adminRequest(`/noshop/videos/${videoId}`, { method: "PUT", body: payload });
      setForm(fromApi(saved));
      setMessage("Video saved.");
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  const previewId = previewYouTubeId(form.youTubeUrl);
  const previewThumbnailFromYouTube = previewId ? `https://img.youtube.com/vi/${previewId}/hqdefault.jpg` : null;
  const displayThumbnail = form.thumbnailUrl ?? previewThumbnailFromYouTube;

  return (
    <form onSubmit={submit} className="vstack gap-4">
      <div className="d-flex align-items-center gap-3">
        <h1 className="h4 mb-0">{isNew ? "New video" : `Edit ${form.title}`}</h1>
        <Link href="/admin/noshop-videos" className="small">Back to videos</Link>
      </div>

      {error && <div className="alert alert-danger py-2 mb-0">{error}</div>}
      {message && <div className="alert alert-success py-2 mb-0">{message}</div>}

      <section className="row g-3">
        <div className="col-md-8">
          <label className="form-label">Title</label>
          <input className="form-control" value={form.title} onChange={(e) => updateTitle(e.target.value)} required maxLength={300} />
        </div>
        <div className="col-md-4">
          <label className="form-label">Category</label>
          <select className="form-select" value={form.categoryId} onChange={(e) => update("categoryId", e.target.value)} required>
            <option value="">Choose...</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
        </div>
        <div className="col-12">
          <label className="form-label">Slug</label>
          <input
            className="form-control"
            value={form.slug}
            onChange={(e) => { setSlugTouched(true); update("slug", e.target.value); }}
            required
            maxLength={320}
          />
        </div>
        <div className="col-12">
          <label className="form-label">YouTube URL</label>
          <input
            className="form-control"
            value={form.youTubeUrl}
            onChange={(e) => update("youTubeUrl", e.target.value)}
            required
            maxLength={500}
            placeholder="https://www.youtube.com/watch?v=... or youtu.be/... or /shorts/..."
          />
          {form.youTubeUrl && !previewId && (
            <div className="form-text text-danger">This doesn&apos;t look like a valid YouTube URL or video ID yet.</div>
          )}
        </div>
        <div className="col-12">
          <label className="form-label">Description</label>
          <textarea className="form-control" rows={3} value={form.description} onChange={(e) => update("description", e.target.value)} maxLength={1000} />
        </div>
      </section>

      {displayThumbnail && (
        <section>
          <h2 className="h6 mb-2">Thumbnail</h2>
          {thumbnailError && <div className="alert alert-danger py-2 mb-2">{thumbnailError}</div>}
          <div className="d-flex align-items-start gap-3">
            <img src={displayThumbnail} alt="Thumbnail" width={160} className="border rounded" style={{ objectFit: "cover" }} />
            {!isNew && (
              <div>
                <input
                  type="file"
                  accept={IMAGE_ACCEPT}
                  className="form-control form-control-sm mb-1"
                  style={{ width: 220 }}
                  disabled={thumbnailBusy}
                  onChange={(e) => {
                    const file = e.target.files?.[0];
                    e.target.value = "";
                    if (file) uploadThumbnail(file);
                  }}
                />
                {thumbnailBusy && <div className="small text-muted">Uploading...</div>}
                {form.thumbnailUrl && form.thumbnailUrl !== previewThumbnailFromYouTube && (
                  <button type="button" className="btn btn-outline-danger btn-sm" onClick={removeThumbnail}>
                    Remove custom thumbnail
                  </button>
                )}
                <p className="small text-muted mb-0 mt-1">
                  Falls back to YouTube&apos;s own thumbnail unless you upload a custom one.
                </p>
              </div>
            )}
          </div>
          {isNew && <p className="small text-muted mt-2 mb-0">Save the video first to upload a custom thumbnail.</p>}
        </section>
      )}

      <section className="row g-3">
        <div className="col-md-3">
          <label className="form-label">Video type</label>
          <select className="form-select" value={form.videoType} onChange={(e) => update("videoType", e.target.value)}>
            {VIDEO_TYPES.map((t) => (
              <option key={t} value={t}>{t}</option>
            ))}
          </select>
        </div>
        <div className="col-md-3">
          <label className="form-label">Status</label>
          <select className="form-select" value={form.status} onChange={(e) => update("status", e.target.value)}>
            {STATUSES.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </div>
        <div className="col-md-3">
          <label className="form-label">Display order</label>
          <input type="number" className="form-control" value={form.displayOrder} onChange={(e) => update("displayOrder", e.target.value)} />
        </div>
        <div className="col-md-3">
          <label className="form-label">Publish date</label>
          <input
            type="datetime-local"
            className="form-control"
            value={form.publishedAt}
            onChange={(e) => update("publishedAt", e.target.value)}
          />
          <div className="form-text">Leave blank with Status = Published to publish immediately.</div>
        </div>
        <div className="col-12">
          <div className="form-check">
            <input id="isFeatured" type="checkbox" className="form-check-input" checked={form.isFeatured} onChange={(e) => update("isFeatured", e.target.checked)} />
            <label htmlFor="isFeatured" className="form-check-label">Featured</label>
          </div>
        </div>
      </section>

      <section className="row g-3">
        <div className="col-md-6">
          <label className="form-label">SEO title</label>
          <input className="form-control" value={form.seoTitle} onChange={(e) => update("seoTitle", e.target.value)} maxLength={200} />
        </div>
        <div className="col-md-6">
          <label className="form-label">SEO description</label>
          <input className="form-control" value={form.seoDescription} onChange={(e) => update("seoDescription", e.target.value)} maxLength={500} />
        </div>
      </section>

      <div>
        <button type="submit" className="btn btn-dark" disabled={busy}>
          {busy ? "Saving..." : isNew ? "Create video" : "Save changes"}
        </button>
      </div>
    </form>
  );
}
