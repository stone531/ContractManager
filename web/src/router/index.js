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
import ContractApprovalView from '../views/ContractApprovalView.vue'
import AmountApprovalView from '../views/AmountApprovalView.vue'
import ContractNotificationView from '../views/ContractNotificationView.vue'
import AmountNotificationView from '../views/AmountNotificationView.vue'
import PaymentApprovalView from '../views/PaymentApprovalView.vue'
import ChangePasswordView from '../views/ChangePasswordView.vue'

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
        path: 'contracts/:id',
        name: 'ContractDetail',
        component: ContractDetailView,
        meta: { requiresAuth: true }
      },
      {
        path: 'approval/contracts',
        name: 'ContractApproval',
        component: ContractApprovalView,
        meta: { requiresAuth: true, requiresSuperAdmin: true }
      },
      {
        path: 'approval/amounts',
        name: 'AmountApproval',
        component: AmountApprovalView,
        meta: { requiresAuth: true, requiresSuperAdmin: true }
      },
      {
        path: 'approval/payments',
        name: 'PaymentApproval',
        component: PaymentApprovalView,
        meta: { requiresAuth: true, requiresSuperAdmin: true }
      },
      {
        path: 'notifications/contract',
        name: 'ContractNotification',
        component: ContractNotificationView,
        meta: { requiresAuth: true }
      },
      {
        path: 'notifications/amount',
        name: 'AmountNotification',
        component: AmountNotificationView,
        meta: { requiresAuth: true }
      },
      {
        path: 'change-password',
        name: 'ChangePassword',
        component: ChangePasswordView,
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
    next('/login')
  } else if ((to.path === '/login' || to.path === '/register') && authStore.isAuthenticated) {
    next('/')
  } else if (to.meta.requiresSuperAdmin) {
    const role = authStore.user?.role
    if (role === 0 || role === 1) {
      next()
    } else {
      next('/')
    }
  } else {
    next()
  }
})

export default router
