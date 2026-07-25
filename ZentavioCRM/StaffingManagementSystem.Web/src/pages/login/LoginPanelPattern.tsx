/** Subtle abstract geometric backdrop for the login promotional panel. Decorative only. */
export function LoginPanelPattern() {
  return (
    <svg aria-hidden="true" focusable="false" viewBox="0 0 600 900" preserveAspectRatio="xMidYMid slice">
      <g fill="none" stroke="rgba(255,255,255,0.08)" strokeWidth="1">
        <circle cx="520" cy="80" r="140" />
        <circle cx="520" cy="80" r="200" />
        <circle cx="60" cy="820" r="120" />
      </g>
      <g fill="rgba(255,255,255,0.035)">
        <rect x="380" y="-40" width="260" height="260" rx="36" transform="rotate(18 510 90)" />
        <rect x="-60" y="700" width="220" height="220" rx="32" transform="rotate(-14 50 810)" />
      </g>
      <g stroke="rgba(229,57,53,0.18)" strokeWidth="1.5">
        <path d="M0 340 L600 260" />
        <path d="M0 400 L600 320" />
      </g>
    </svg>
  );
}
