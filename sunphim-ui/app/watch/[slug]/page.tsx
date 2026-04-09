// ============================================================
// SunPhim — Watch Page
// ============================================================
import { WatchClient } from "@/components/watch/WatchClient";

interface Props {
  params: Promise<{ slug: string }>;
}

// Required for static export; empty array = all slugs rendered on-demand at runtime
export function generateStaticParams() {
  return [];
}

export default async function WatchPage({ params }: Props) {
  const { slug } = await params;
  return <WatchClient slug={slug} />;
}
