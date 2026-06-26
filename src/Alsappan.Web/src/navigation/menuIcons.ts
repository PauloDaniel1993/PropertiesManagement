import type { LucideIcon } from 'lucide-react'
import {
  Bell,
  CarFront,
  ChartNoAxesCombined,
  CircleAlert,
  CircleDollarSign,
  ClipboardCheck,
  FileText,
  FolderOpen,
  House,
  LayoutDashboard,
  PawPrint,
  ReceiptText,
  SearchCheck,
  Settings,
  ShieldCheck,
  Users,
} from 'lucide-react'
import type { MenuIconId } from './menuContract'

export const menuIconComponents: Record<MenuIconId, LucideIcon> = {
  bell: Bell,
  'car-front': CarFront,
  'chart-no-axes-combined': ChartNoAxesCombined,
  'circle-alert': CircleAlert,
  'circle-dollar-sign': CircleDollarSign,
  'clipboard-check': ClipboardCheck,
  'file-text': FileText,
  'folder-open': FolderOpen,
  house: House,
  'layout-dashboard': LayoutDashboard,
  'paw-print': PawPrint,
  'receipt-text': ReceiptText,
  'search-check': SearchCheck,
  settings: Settings,
  'shield-check': ShieldCheck,
  users: Users,
}
