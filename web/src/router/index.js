import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'
import MainLayout from '../layouts/MainLayout.vue'
import DashboardView from '../views/DashboardView.vue'
import UserAddView from '../views/UserAddView.vue'
import UserListView from '../views/UserListView.vue'
import UserEditView from '../views/UserEditView.vue'
import ContractListView from '../views/ContractListView.vue'
import ContractAddView from '../views/ContractAddView.vue'
import ContractDetailView from '../views/ContractDetailView.vue'
import ContractEditView from '../views/ContractEditView.vue'
import ApprovalManagementView from '../views/ApprovalManagementView.vue'
import NotificationView from '../views/NotificationView.vue'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: LoginView,
    meta: { requiresAuth: false }
  },
  {
    path: '/register',
    name: 'Register',
    component: RegisterView,
    meta: { requiresAuth: false }
  },
  {
    path: '/',
    component: MainLayout,
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'Dashboard',
        component: DashboardView,
        meta: { requiresAuth: true }
      },
      {
        path: 'users/add',
        name: 'UserAdd',
        component: UserAddView,
        meta: { requiresAuth: true }
      },
      {
        path: 'users/list',
        name: 'UserList',
        component: UserListView,
        meta: { requiresAuth: true }
      },
      {
        path: 'users/edit/:id',
        name: 'UserEdit',
        component: UserEditView,
        meta: { requiresAuth: true }
      },
      {
        path: 'contracts/list',
        name: 'ContractList',
        component: ContractListView,
        meta: { requiresAuth: true }
      },
      {
        path: 'contracts/add',
        name: 'ContractAdd',
        component: ContractAddView,
        meta: { requiresAuth: true }
      },
      {
        path: 'contracts/edit/:id',
        name: 'ContractEdit',
        component: ContractEditView,
        meta: { requiresAuth: true }
      },
      {
        path: 'contracts/approval',
        name: 'ApprovalManagement',
        component: ApprovalManagementView,
        meta: { requiresAuth: true }
      },
      {
        path: 'notifications',
        name: 'Notifications',
        component: NotificationView,
        meta: { requiresAuth: true }
      },
      {
        path: 'contracts/:id',
        name: 'ContractDetail',
        component: ContractDetailView,
        meta: { requiresAuth: true }
      }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 导航守卫
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    // 需要认证但未登录，重定向到登录页
    next('/login')
  } else if ((to.path === '/login' || to.path === '/register') && authStore.isAuthenticated) {
    // 已登录用户访问登录/注册页，重定向到首页
    next('/')
  } else {
    next()
  }
})

export default router
