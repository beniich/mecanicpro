// components/services/ServiceIcons.tsx
// All 15 hand-drawn SVG icons for the atelier services menu

import type { SVGProps } from 'react'

type IconProps = SVGProps<SVGSVGElement> & { size?: number }

export function IconReception({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <rect x="8" y="32" width="40" height="16" rx="3" fill="#BA7517"/>
      <rect x="12" y="28" width="32" height="8" rx="2" fill="#EF9F27"/>
      <rect x="14" y="24" width="28" height="6" rx="2" fill="#FAC775"/>
      <ellipse cx="18" cy="48" rx="5" ry="5" fill="#2C2C2A"/>
      <ellipse cx="18" cy="48" rx="2.5" ry="2.5" fill="#888780"/>
      <ellipse cx="38" cy="48" rx="5" ry="5" fill="#2C2C2A"/>
      <ellipse cx="38" cy="48" rx="2.5" ry="2.5" fill="#888780"/>
      <rect x="24" y="26" width="8" height="5" rx="1" fill="#B5D4F4"/>
      <rect x="6" y="35" width="5" height="8" rx="1" fill="#EF9F27"/>
      <rect x="45" y="35" width="5" height="8" rx="1" fill="#EF9F27"/>
      <rect x="30" y="34" width="10" height="5" rx="1" fill="#854F0B"/>
    </svg>
  )
}

export function IconLevage({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <rect x="24" y="4" width="8" height="30" rx="2" fill="#5F5E5A"/>
      <rect x="20" y="10" width="16" height="4" rx="1" fill="#888780"/>
      <ellipse cx="28" cy="36" rx="6" ry="4" fill="#BA7517"/>
      <ellipse cx="28" cy="36" rx="3" ry="2" fill="#EF9F27"/>
      <rect x="10" y="34" width="36" height="14" rx="3" fill="#EF9F27"/>
      <rect x="8" y="40" width="40" height="8" rx="2" fill="#BA7517"/>
      <rect x="14" y="36" width="28" height="4" rx="1" fill="#FAC775"/>
      <ellipse cx="16" cy="48" rx="4" ry="4" fill="#2C2C2A"/>
      <ellipse cx="16" cy="48" rx="2" ry="2" fill="#888780"/>
      <ellipse cx="40" cy="48" rx="4" ry="4" fill="#2C2C2A"/>
      <ellipse cx="40" cy="48" rx="2" ry="2" fill="#888780"/>
      <rect x="22" y="34" width="5" height="3" rx="1" fill="#EF9F27"/>
      <circle cx="28" cy="20" r="4" fill="#2C2C2A"/>
      <circle cx="28" cy="20" r="2" fill="#B4B2A9"/>
    </svg>
  )
}

export function IconPeinture({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <rect x="14" y="20" width="28" height="18" rx="3" fill="#3C3489"/>
      <rect x="18" y="14" width="20" height="8" rx="2" fill="#534AB7"/>
      <rect x="20" y="16" width="16" height="5" rx="1" fill="#85B7EB"/>
      <ellipse cx="20" cy="38" rx="5" ry="5" fill="#2C2C2A"/>
      <ellipse cx="20" cy="38" rx="2.5" ry="2.5" fill="#888780"/>
      <ellipse cx="36" cy="38" rx="5" ry="5" fill="#2C2C2A"/>
      <ellipse cx="36" cy="38" rx="2.5" ry="2.5" fill="#888780"/>
      <rect x="6" y="8" width="4" height="28" rx="1" fill="#888780"/>
      <rect x="46" y="8" width="4" height="28" rx="1" fill="#888780"/>
      <rect x="6" y="10" width="44" height="3" rx="1" fill="#B4B2A9"/>
      <rect x="6" y="30" width="44" height="3" rx="1" fill="#B4B2A9"/>
      <circle cx="28" cy="22" r="3" fill="#E24B4A"/>
      <circle cx="28" cy="22" r="1.5" fill="#F09595"/>
    </svg>
  )
}

export function IconPneumatiques({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <ellipse cx="28" cy="34" rx="18" ry="8" fill="#444441"/>
      <ellipse cx="28" cy="32" rx="18" ry="8" fill="#5F5E5A"/>
      <ellipse cx="28" cy="30" rx="14" ry="6" fill="#888780"/>
      <ellipse cx="28" cy="28" rx="14" ry="6" fill="#B4B2A9"/>
      <ellipse cx="28" cy="28" rx="10" ry="4" fill="#D3D1C7"/>
      <ellipse cx="28" cy="28" rx="6" ry="2.5" fill="#888780"/>
      <ellipse cx="28" cy="28" rx="3" ry="1.5" fill="#444441"/>
      <rect x="12" y="10" width="32" height="6" rx="2" fill="#5F5E5A"/>
      <rect x="14" y="8" width="28" height="4" rx="1" fill="#888780"/>
      <rect x="20" y="6" width="16" height="4" rx="1" fill="#B4B2A9"/>
    </svg>
  )
}

export function IconGarage({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <rect x="6" y="16" width="44" height="28" rx="3" fill="#185FA5"/>
      <rect x="10" y="20" width="18" height="16" rx="2" fill="#085041" opacity="0.8"/>
      <rect x="30" y="20" width="16" height="16" rx="2" fill="#085041" opacity="0.8"/>
      <rect x="10" y="20" width="18" height="16" rx="2" fill="#E1F5EE" opacity=".2"/>
      <rect x="30" y="20" width="16" height="16" rx="2" fill="#E1F5EE" opacity=".2"/>
      <rect x="6" y="40" width="44" height="4" rx="1" fill="#0C447C"/>
      <rect x="22" y="12" width="12" height="6" rx="1" fill="#185FA5"/>
      <rect x="24" y="8" width="8" height="6" rx="1" fill="#0C447C"/>
      <rect x="6" y="42" width="10" height="8" rx="1" fill="#B4B2A9"/>
      <rect x="40" y="42" width="10" height="8" rx="1" fill="#B4B2A9"/>
      <rect x="14" y="12" width="4" height="6" rx="1" fill="#B4B2A9"/>
      <rect x="38" y="12" width="4" height="6" rx="1" fill="#B4B2A9"/>
      <rect x="24" y="32" width="8" height="8" rx="1" fill="#EF9F27"/>
    </svg>
  )
}

export function IconAssistance({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <rect x="10" y="28" width="36" height="20" rx="3" fill="#BA7517"/>
      <rect x="14" y="22" width="28" height="10" rx="2" fill="#EF9F27"/>
      <rect x="16" y="18" width="24" height="6" rx="2" fill="#FAC775"/>
      <ellipse cx="19" cy="48" rx="5" ry="5" fill="#2C2C2A"/>
      <ellipse cx="19" cy="48" rx="2.5" ry="2.5" fill="#888780"/>
      <ellipse cx="37" cy="48" rx="5" ry="5" fill="#2C2C2A"/>
      <ellipse cx="37" cy="48" rx="2.5" ry="2.5" fill="#888780"/>
      <rect x="20" y="20" width="16" height="5" rx="1" fill="#B5D4F4"/>
      <rect x="18" y="30" width="8" height="8" rx="1" fill="#FAC775"/>
      <rect x="32" y="30" width="8" height="8" rx="1" fill="#FAC775"/>
      <rect x="8" y="32" width="6" height="6" rx="1" fill="#EF9F27"/>
      <circle cx="28" cy="14" r="5" fill="#E24B4A"/>
      <path d="M28 10 L30 14 L28 18 L26 14 Z" fill="#F09595"/>
      <circle cx="28" cy="14" r="2" fill="#A32D2D"/>
    </svg>
  )
}

export function IconRemorquage({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <rect x="8" y="26" width="24" height="24" rx="3" fill="#BA7517"/>
      <rect x="10" y="20" width="20" height="8" rx="2" fill="#EF9F27"/>
      <rect x="12" y="16" width="16" height="6" rx="2" fill="#FAC775"/>
      <ellipse cx="15" cy="50" rx="5" ry="5" fill="#2C2C2A"/>
      <ellipse cx="15" cy="50" rx="2.5" ry="2.5" fill="#888780"/>
      <ellipse cx="27" cy="50" rx="5" ry="5" fill="#2C2C2A"/>
      <ellipse cx="27" cy="50" rx="2.5" ry="2.5" fill="#888780"/>
      <rect x="30" y="30" width="22" height="14" rx="2" fill="#5F5E5A"/>
      <rect x="30" y="34" width="22" height="6" rx="1" fill="#888780"/>
      <rect x="32" y="36" width="18" height="2" rx="1" fill="#B4B2A9"/>
      <ellipse cx="35" cy="44" rx="4" ry="4" fill="#2C2C2A"/>
      <ellipse cx="35" cy="44" rx="2" ry="2" fill="#888780"/>
      <ellipse cx="47" cy="44" rx="4" ry="4" fill="#2C2C2A"/>
      <ellipse cx="47" cy="44" rx="2" ry="2" fill="#888780"/>
    </svg>
  )
}

export function IconUrgence({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <rect x="20" y="6" width="16" height="32" rx="2" fill="#185FA5"/>
      <rect x="22" y="8" width="12" height="28" rx="1" fill="#0C447C"/>
      <rect x="24" y="10" width="8" height="8" rx="1" fill="#B5D4F4"/>
      <rect x="23" y="22" width="10" height="4" rx="1" fill="#B4B2A9"/>
      <rect x="23" y="28" width="10" height="4" rx="1" fill="#B4B2A9"/>
      <rect x="12" y="36" width="32" height="14" rx="2" fill="#1D9E75"/>
      <rect x="14" y="40" width="28" height="6" rx="1" fill="#9FE1CB"/>
      <ellipse cx="18" cy="50" rx="4" ry="4" fill="#2C2C2A"/>
      <ellipse cx="38" cy="50" rx="4" ry="4" fill="#2C2C2A"/>
      <ellipse cx="18" cy="50" rx="2" ry="2" fill="#888780"/>
      <ellipse cx="38" cy="50" rx="2" ry="2" fill="#888780"/>
      <circle cx="28" cy="18" r="2" fill="#E24B4A"/>
    </svg>
  )
}

export function IconControle({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <circle cx="28" cy="22" r="14" fill="#B4B2A9"/>
      <circle cx="28" cy="22" r="10" fill="#D3D1C7"/>
      <circle cx="28" cy="22" r="6" fill="#F1EFE8"/>
      <circle cx="28" cy="22" r="3" fill="#5F5E5A"/>
      <rect x="27" y="10" width="2" height="6" rx="1" fill="#444441"/>
      <rect x="27" y="28" width="2" height="6" rx="1" fill="#444441"/>
      <rect x="16" y="21" width="6" height="2" rx="1" fill="#444441"/>
      <rect x="34" y="21" width="6" height="2" rx="1" fill="#444441"/>
      <rect x="26" y="20" width="6" height="2" rx="1" fill="#2C2C2A"/>
      <rect x="26" y="18" width="2" height="6" rx="1" fill="#2C2C2A"/>
      <rect x="8" y="38" width="40" height="4" rx="2" fill="#444441"/>
      <rect x="6" y="40" width="44" height="8" rx="2" fill="#5F5E5A"/>
      <rect x="10" y="42" width="36" height="4" rx="1" fill="#888780"/>
      <ellipse cx="16" cy="44" rx="3" ry="3" fill="#2C2C2A"/>
      <ellipse cx="40" cy="44" rx="3" ry="3" fill="#2C2C2A"/>
    </svg>
  )
}

export function IconVoitureService({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <rect x="10" y="28" width="36" height="18" rx="3" fill="#639922"/>
      <rect x="14" y="22" width="28" height="10" rx="2" fill="#97C459"/>
      <rect x="18" y="18" width="20" height="6" rx="2" fill="#C0DD97"/>
      <ellipse cx="18" cy="46" rx="5" ry="5" fill="#2C2C2A"/>
      <ellipse cx="18" cy="46" rx="2.5" ry="2.5" fill="#888780"/>
      <ellipse cx="38" cy="46" rx="5" ry="5" fill="#2C2C2A"/>
      <ellipse cx="38" cy="46" rx="2.5" ry="2.5" fill="#888780"/>
      <rect x="16" y="20" width="10" height="5" rx="1" fill="#85B7EB"/>
      <rect x="30" y="30" width="10" height="8" rx="1" fill="#C0DD97"/>
      <rect x="10" y="30" width="8" height="8" rx="1" fill="#C0DD97"/>
      <circle cx="28" cy="14" r="5" fill="#97C459"/>
      <rect x="26" y="8" width="4" height="8" rx="1" fill="#3B6D11"/>
      <rect x="24" y="11" width="8" height="4" rx="1" fill="#3B6D11"/>
    </svg>
  )
}

export function IconTaxi({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <rect x="10" y="28" width="36" height="18" rx="3" fill="#EF9F27"/>
      <rect x="14" y="22" width="28" height="10" rx="2" fill="#FAC775"/>
      <rect x="18" y="18" width="20" height="6" rx="2" fill="#FAEEDA"/>
      <ellipse cx="18" cy="46" rx="5" ry="5" fill="#2C2C2A"/>
      <ellipse cx="18" cy="46" rx="2.5" ry="2.5" fill="#888780"/>
      <ellipse cx="38" cy="46" rx="5" ry="5" fill="#2C2C2A"/>
      <ellipse cx="38" cy="46" rx="2.5" ry="2.5" fill="#888780"/>
      <rect x="16" y="20" width="10" height="5" rx="1" fill="#85B7EB"/>
      <rect x="20" y="24" width="16" height="4" rx="1" fill="#85B7EB"/>
      <rect x="8" y="32" width="40" height="4" rx="1" fill="#2C2C2A" opacity="0.15"/>
      <rect x="22" y="14" width="12" height="6" rx="2" fill="#2C2C2A"/>
      <rect x="24" y="15" width="8" height="4" rx="1" fill="#EF9F27"/>
    </svg>
  )
}

export function IconMecanicien({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <circle cx="28" cy="12" r="8" fill="#FAC775"/>
      <rect x="20" y="18" width="16" height="22" rx="3" fill="#185FA5"/>
      <rect x="16" y="24" width="8" height="14" rx="2" fill="#185FA5"/>
      <rect x="32" y="24" width="8" height="14" rx="2" fill="#185FA5"/>
      <rect x="22" y="38" width="5" height="12" rx="2" fill="#0C447C"/>
      <rect x="29" y="38" width="5" height="12" rx="2" fill="#0C447C"/>
      <rect x="22" y="22" width="12" height="6" rx="1" fill="#EF9F27"/>
      <circle cx="28" cy="10" r="3" fill="#EF9F27"/>
      <rect x="16" y="36" width="8" height="4" rx="1" fill="#888780"/>
      <rect x="32" y="36" width="8" height="4" rx="1" fill="#888780"/>
    </svg>
  )
}

export function IconServiceClient({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <rect x="20" y="4" width="16" height="32" rx="3" fill="#185FA5"/>
      <rect x="23" y="7" width="10" height="14" rx="2" fill="#B5D4F4"/>
      <rect x="23" y="24" width="10" height="4" rx="1" fill="#D3D1C7"/>
      <rect x="23" y="30" width="10" height="4" rx="1" fill="#D3D1C7"/>
      <rect x="23" y="36" width="10" height="4" rx="1" fill="#D3D1C7"/>
      <circle cx="28" cy="12" r="3" fill="#E24B4A" opacity=".8"/>
      <rect x="14" y="44" width="28" height="8" rx="2" fill="#0C447C"/>
      <circle cx="22" cy="8" r="2" fill="#85B7EB"/>
    </svg>
  )
}

export function IconDiagnostic({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <rect x="6" y="14" width="18" height="24" rx="2" fill="#444441"/>
      <rect x="8" y="16" width="14" height="20" rx="1" fill="#2C2C2A"/>
      <rect x="9" y="17" width="12" height="8" rx="1" fill="#1D9E75" opacity=".7"/>
      <rect x="9" y="27" width="5" height="2" rx="1" fill="#888780"/>
      <rect x="9" y="31" width="5" height="2" rx="1" fill="#888780"/>
      <rect x="16" y="27" width="5" height="2" rx="1" fill="#888780"/>
      <rect x="16" y="31" width="5" height="2" rx="1" fill="#888780"/>
      <rect x="28" y="18" width="22" height="20" rx="2" fill="#5F5E5A"/>
      <rect x="30" y="20" width="18" height="14" rx="1" fill="#BA7517" opacity=".8"/>
      <rect x="32" y="22" width="14" height="10" rx="1" fill="#EF9F27" opacity=".6"/>
      <rect x="34" y="24" width="6" height="6" rx="1" fill="#FAC775"/>
      <rect x="6" y="38" width="44" height="4" rx="1" fill="#B4B2A9"/>
    </svg>
  )
}

export function IconMoteur({ size = 56, ...props }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 56 56" fill="none" {...props}>
      <rect x="24" y="6" width="8" height="18" rx="2" fill="#5F5E5A"/>
      <rect x="26" y="8" width="4" height="14" rx="1" fill="#888780"/>
      <ellipse cx="28" cy="26" rx="8" ry="6" fill="#BA7517"/>
      <ellipse cx="28" cy="26" rx="5" ry="4" fill="#EF9F27"/>
      <ellipse cx="28" cy="26" rx="2.5" ry="2" fill="#FAC775"/>
      <rect x="6" y="32" width="10" height="16" rx="2" fill="#444441"/>
      <rect x="8" y="34" width="6" height="12" rx="1" fill="#2C2C2A"/>
      <rect x="40" y="32" width="10" height="16" rx="2" fill="#444441"/>
      <rect x="42" y="34" width="6" height="12" rx="1" fill="#2C2C2A"/>
      <rect x="14" y="36" width="28" height="12" rx="2" fill="#5F5E5A"/>
      <rect x="16" y="38" width="24" height="8" rx="1" fill="#888780"/>
      <rect x="18" y="40" width="20" height="4" rx="1" fill="#B4B2A9"/>
    </svg>
  )
}

// ─── Icon map for dynamic lookup ─────────────────────────────
export const SERVICE_ICONS: Record<string, React.FC<IconProps>> = {
  reception: IconReception,
  levage: IconLevage,
  peinture: IconPeinture,
  pneumatiques: IconPneumatiques,
  garage: IconGarage,
  assistance: IconAssistance,
  remorquage: IconRemorquage,
  urgence: IconUrgence,
  controle: IconControle,
  voiture_service: IconVoitureService,
  taxi: IconTaxi,
  mecanicien: IconMecanicien,
  service_client: IconServiceClient,
  diagnostic: IconDiagnostic,
  moteur: IconMoteur,
}
