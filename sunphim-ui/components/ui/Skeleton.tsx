// ============================================================
// SunPhim — Skeleton Loader
// ============================================================
import { cn } from "@/lib/utils";

interface SkeletonProps {
  className?: string;
}

export function Skeleton({ className }: SkeletonProps) {
  return <div className={cn("skeleton", className)} />;
}

export function MovieCardSkeleton() {
  return (
    <div className="flex-none w-[calc((100%-var(--card-gap,16px)*5)/6)]">
      <div className="aspect-[2/3] rounded-md overflow-hidden">
        <Skeleton className="w-full h-full rounded-md" />
      </div>
      <div className="mt-2 space-y-1">
        <Skeleton className="h-3 w-3/4" />
        <Skeleton className="h-2 w-1/2" />
      </div>
    </div>
  );
}

export function HeroSkeleton() {
  return (
    <div className="relative h-[70vh] min-h-[420px] bg-black flex items-end">
      <div className="absolute inset-0">
        <Skeleton className="w-full h-full" />
      </div>
      <div className="relative z-10 p-8 md:p-16 w-full max-w-2xl space-y-4">
        <Skeleton className="h-10 w-2/3" />
        <div className="flex gap-2">
          <Skeleton className="h-4 w-16" />
          <Skeleton className="h-4 w-20" />
          <Skeleton className="h-4 w-12" />
        </div>
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-4 w-2/3" />
        <div className="flex gap-3 mt-4">
          <Skeleton className="h-10 w-32" />
          <Skeleton className="h-10 w-32" />
        </div>
      </div>
    </div>
  );
}

export function MovieRowSkeleton() {
  return (
    <div className="py-4">
      <div className="flex items-center justify-between mb-3 px-[4%]">
        <Skeleton className="h-6 w-40" />
      </div>
      <div className="flex gap-3 overflow-hidden px-[4%]">
        {Array.from({ length: 8 }).map((_, i) => (
          <MovieCardSkeleton key={i} />
        ))}
      </div>
    </div>
  );
}

export function MovieDetailSkeleton() {
  return (
    <div className="pt-20">
      <div className="relative h-[60vh] bg-black">
        <Skeleton className="w-full h-full opacity-50" />
      </div>
      <div className="max-w-7xl mx-auto px-4 -mt-60 relative z-10">
        <div className="flex flex-col md:flex-row gap-8">
          <div className="flex-none w-64">
            <Skeleton className="w-64 aspect-[2/3] rounded-lg" />
          </div>
          <div className="flex-1 space-y-4">
            <Skeleton className="h-10 w-1/2" />
            <Skeleton className="h-4 w-1/3" />
            <Skeleton className="h-20 w-full" />
            <div className="flex gap-3">
              <Skeleton className="h-10 w-32" />
              <Skeleton className="h-10 w-32" />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
