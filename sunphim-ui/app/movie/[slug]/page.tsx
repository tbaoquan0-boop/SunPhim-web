// ============================================================
// SunPhim — Movie Detail Page
// ============================================================
import { MovieDetailClient } from "@/components/movie/MovieDetailClient";

interface Props {
  params: Promise<{ slug: string }>;
}

// Required for static export; empty array = all slugs rendered on-demand at runtime
export function generateStaticParams() {
  return [];
}

export default async function MovieDetailPage({ params }: Props) {
  const { slug } = await params;
  return <MovieDetailClient slug={slug} />;
}
