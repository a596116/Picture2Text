import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'Home',
    component: () => import('../views/Home.vue'),
  },
  {
    path: '/workspace',
    name: 'Workspace',
    component: () => import('../views/Workspace.vue'),
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

export default router
