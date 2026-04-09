// ============================================================
// SunPhim — Category Page
// ============================================================
import { CategoryContent } from "@/components/movie/CategoryClient";

interface Props {
  params: Promise<{ slug: string }>;
}

// Required for static export; empty array = all slugs rendered on-demand at runtime
export function generateStaticParams() {
  return [];
}

export default async function CategoryPage({ params }: Props) {
  const { slug } = await params;
  return <CategoryContent slug={slug} />;
}
