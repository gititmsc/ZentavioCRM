import logoUrl from "@/assets/logo.png";

interface ZentavioLogoProps {
  /** Rendered height of the logo in pixels. Width scales proportionally. */
  height?: number;
  /**
   * "light" wraps the logo in a white chip for contrast when placed on the
   * navy gradient panel. "dark" renders the logo directly (for white backgrounds).
   */
  variant?: "light" | "dark";
  className?: string;
}

/** Zentavio brand mark. */
export function ZentavioLogo({ height = 40, variant = "dark", className }: ZentavioLogoProps) {
  const image = <img src={logoUrl} alt="Zentavio" height={height} style={{ display: "block" }} />;

  if (variant === "light") {
    return (
      <div className={`d-inline-flex align-items-center bg-white rounded-3 px-3 py-2 shadow-sm ${className ?? ""}`}>
        {image}
      </div>
    );
  }

  return <div className={className}>{image}</div>;
}
