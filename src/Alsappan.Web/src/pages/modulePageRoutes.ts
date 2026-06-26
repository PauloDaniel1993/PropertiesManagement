import { adminMenuItems } from '../navigation/menuContract'

export const modulePageRoutes = adminMenuItems.map((item) => ({
  item,
  pathSegment: item.path.replace(/^\//, ''),
}))
