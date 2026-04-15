<template>
  <div class="main-layout">
    <!-- 顶部导航栏 -->
    <header class="header">
      <div class="header-left">
        <button class="menu-toggle" @click="toggleSidebar">
          <span>☰</span>
        </button>
        <h1>合同管理系统</h1>
      </div>
      <div class="header-right">
        <span class="welcome">欢迎, {{ authStore.user?.name }}</span>
        <button @click="goHome" class="home-btn">🏠 首页</button>
        <button @click="goChangePassword" class="home-btn">🔑 修改密码</button>
        <button @click="handleLogout" class="logout-btn">注销</button>
      </div>
    </header>

    <!-- 主体容器 -->
    <div class="main-container">
      <!-- 侧边栏 -->
      <aside class="sidebar" :class="{ collapsed: sidebarCollapsed }">
        <nav class="menu">
          <!-- 用户管理（仅 SuperAdmin 显示） -->
          <div v-if="isSuperAdmin" class="menu-group">
            <!-- 可点击的菜单组标题 -->
            <div 
              class="menu-group-header" 
              @click="toggleMenuGroup('userManagement')"
              :class="{ collapsed: !menuGroups.userManagement }"
            >
              <span class="group-icon">👥</span>
              <span class="group-title">用户管理</span>
              <span class="arrow-icon" :class="{ expanded: menuGroups.userManagement }">
                <svg width="12" height="12" viewBox="0 0 12 12" fill="currentColor">
                  <path d="M2 4l4 4 4-4" stroke="currentColor" stroke-width="1.5" fill="none" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </span>
            </div>
            
            <!-- 子菜单项（带折叠动画） -->
            <transition name="slide-fade">
              <div v-show="menuGroups.userManagement" class="menu-items">
                <router-link to="/users/add" class="menu-item">
                  <span class="menu-icon">📝</span>
                  <span class="menu-text">新增用户</span>
                </router-link>
                <router-link to="/users/list" class="menu-item">
                  <span class="menu-icon">📋</span>
                  <span class="menu-text">查询用户</span>
                </router-link>
              </div>
            </transition>
          </div>

          <!-- 合同管理 -->
          <div class="menu-group">
            <div 
              class="menu-group-header" 
              @click="toggleMenuGroup('contractManagement')"
              :class="{ collapsed: !menuGroups.contractManagement }"
            >
              <span class="group-icon">📄</span>
              <span class="group-title">合同管理</span>
              <span class="arrow-icon" :class="{ expanded: menuGroups.contractManagement }">
                <svg width="12" height="12" viewBox="0 0 12 12" fill="currentColor">
                  <path d="M2 4l4 4 4-4" stroke="currentColor" stroke-width="1.5" fill="none" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </span>
            </div>
            
            <transition name="slide-fade">
              <div v-show="menuGroups.contractManagement" class="menu-items">
                <router-link to="/contracts/add" class="menu-item">
                  <span class="menu-icon">➕</span>
                  <span class="menu-text">增加合同</span>
                </router-link>
                <router-link to="/contracts/list" class="menu-item">
                  <span class="menu-icon">📋</span>
                  <span class="menu-text">合同查询</span>
                </router-link>
              </div>
            </transition>
          </div>

          <!-- 审批管理（仅 SuperAdmin 显示） -->
          <div v-if="isSuperAdmin" class="menu-group">
            <div 
              class="menu-group-header" 
              @click="toggleMenuGroup('approvalManagement')"
              :class="{ collapsed: !menuGroups.approvalManagement }"
            >
              <span class="group-icon">✅</span>
              <span class="group-title">审批管理</span>
              <span class="arrow-icon" :class="{ expanded: menuGroups.approvalManagement }">
                <svg width="12" height="12" viewBox="0 0 12 12" fill="currentColor">
                  <path d="M2 4l4 4 4-4" stroke="currentColor" stroke-width="1.5" fill="none" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </span>
            </div>
            <transition name="slide-fade">
              <div v-show="menuGroups.approvalManagement" class="menu-items">
                <router-link to="/approval/contracts" class="menu-item">
                  <span class="menu-icon">📋</span>
                  <span class="menu-text">合同审批</span>
                </router-link>
                <router-link to="/approval/amounts" class="menu-item">
                  <span class="menu-icon">💰</span>
                  <span class="menu-text">金额审批</span>
                </router-link>
                <router-link to="/approval/payments" class="menu-item">
                  <span class="menu-icon">💳</span>
                  <span class="menu-text">支付审批</span>
                </router-link>
              </div>
            </transition>
          </div>

          <!-- 通知管理（所有角色可见） -->
          <div class="menu-group">
            <div 
              class="menu-group-header" 
              @click="toggleMenuGroup('notificationManagement')"
              :class="{ collapsed: !menuGroups.notificationManagement }"
            >
              <span class="group-icon">🔔</span>
              <span class="group-title">通知管理</span>
              <span v-if="unreadCount > 0" class="badge">{{ unreadCount }}</span>
              <span class="arrow-icon" :class="{ expanded: menuGroups.notificationManagement }">
                <svg width="12" height="12" viewBox="0 0 12 12" fill="currentColor">
                  <path d="M2 4l4 4 4-4" stroke="currentColor" stroke-width="1.5" fill="none" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </span>
            </div>
            <transition name="slide-fade">
              <div v-show="menuGroups.notificationManagement" class="menu-items">
                <router-link to="/notifications/contract" class="menu-item">
                  <span class="menu-icon">📄</span>
                  <span class="menu-text">合同消息</span>
                  <span v-if="contractUnread > 0" class="sub-badge">{{ contractUnread }}</span>
                </router-link>
                <router-link to="/notifications/amount" class="menu-item">
                  <span class="menu-icon">💰</span>
                  <span class="menu-text">金额消息</span>
                  <span v-if="amountUnread > 0" class="sub-badge">{{ amountUnread }}</span>
                </router-link>
              </div>
            </transition>
          </div>
        </nav>
      </aside>

      <!-- 主内容区 -->
      <main class="content">
        <router-view></router-view>
      </main>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, provide } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import apiClient from '../api/axios'

const router = useRouter()
const authStore = useAuthStore()
const sidebarCollapsed = ref(false)
const contractUnread = ref(0)
const amountUnread = ref(0)
const unreadCount = computed(() => contractUnread.value + amountUnread.value)

const isSuperAdmin = computed(() => {
  const role = authStore.user?.role
  return role === 0 || role === 'SuperAdmin'
})

async function fetchUnreadCount() {
  try {
    const res = await apiClient.get('/notifications/unread-count-by-category')
    contractUnread.value = res.data.contract || 0
    amountUnread.value = res.data.amount || 0
  } catch (e) { /* ignore */ }
}

// 提供刷新函数给子组件
provide('refreshUnreadCount', fetchUnreadCount)

// 每30秒刷新未读数
onMounted(() => {
  fetchUnreadCount()
  setInterval(fetchUnreadCount, 3600000)
})

// 菜单组展开状态
const menuGroups = ref({
  userManagement: true,
  contractManagement: true,
  approvalManagement: true,
  notificationManagement: true
})

function toggleSidebar() {
  sidebarCollapsed.value = !sidebarCollapsed.value
}

function toggleMenuGroup(groupName) {
  menuGroups.value[groupName] = !menuGroups.value[groupName]
}

function goHome() {
  router.push('/')
}

function goChangePassword() {
  router.push('/change-password')
}

function handleLogout() {
  authStore.logout()
  router.push('/login')
}
</script>

<style scoped>
.main-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f5f7fa;
}

/* 顶部导航栏 */
.header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 0 24px;
  height: 60px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  position: sticky;
  top: 0;
  z-index: 100;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.menu-toggle {
  background: none;
  border: none;
  color: white;
  font-size: 24px;
  cursor: pointer;
  padding: 8px;
  border-radius: 4px;
  transition: background 0.3s;
}

.menu-toggle:hover {
  background: rgba(255, 255, 255, 0.1);
}

.header h1 {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.welcome {
  font-size: 14px;
}

.home-btn,
.logout-btn {
  padding: 8px 16px;
  background: rgba(255, 255, 255, 0.2);
  color: white;
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 6px;
  cursor: pointer;
  transition: background 0.3s;
  font-size: 14px;
}

.home-btn:hover,
.logout-btn:hover {
  background: rgba(255, 255, 255, 0.3);
}

/* 主体容器 */
.main-container {
  display: flex;
  flex: 1;
  overflow: hidden;
}

/* 侧边栏 */
.sidebar {
  width: 240px;
  background: white;
  box-shadow: 2px 0 8px rgba(0, 0, 0, 0.05);
  transition: width 0.3s ease;
  overflow-y: auto;
  overflow-x: hidden;
}

.sidebar.collapsed {
  width: 70px;
}

.sidebar.collapsed .menu-text,
.sidebar.collapsed .group-title,
.sidebar.collapsed .arrow-icon {
  opacity: 0;
  width: 0;
  overflow: hidden;
}

.sidebar.collapsed .menu-group-header {
  justify-content: center;
  padding: 12px 8px;
}

.sidebar.collapsed .menu-item {
  justify-content: center;
  padding: 10px 8px;
  margin: 2px 8px;
}

.sidebar.collapsed .menu-item::before {
  display: none;
}

.menu {
  padding: 12px 8px;
}

.menu-group {
  margin-bottom: 8px;
}

/* 菜单组标题（可点击） */
.menu-group-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  margin: 4px 8px;
  cursor: pointer;
  border-radius: 10px;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.08) 0%, rgba(118, 75, 162, 0.08) 100%);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  user-select: none;
}

.menu-group-header:hover {
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.15) 0%, rgba(118, 75, 162, 0.15) 100%);
  transform: translateX(4px);
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.15);
}

.menu-group-header:active {
  transform: translateX(2px);
}

.group-icon {
  font-size: 18px;
  margin-right: 10px;
}

.group-title {
  flex: 1;
  font-size: 13px;
  font-weight: 600;
  color: #2c3e50;
  letter-spacing: 0.3px;
}

.arrow-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  color: #667eea;
}

.arrow-icon.expanded {
  transform: rotate(180deg);
}

/* 子菜单容器 */
.menu-items {
  padding: 4px 0;
}

/* 菜单项 */
.menu-item {
  display: flex;
  align-items: center;
  padding: 10px 16px;
  margin: 2px 12px 2px 20px;
  color: #5a6c7d;
  text-decoration: none;
  border-radius: 8px;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  gap: 12px;
  position: relative;
  font-size: 14px;
}

/* 菜单项左侧装饰线 */
.menu-item::before {
  content: '';
  position: absolute;
  left: -8px;
  top: 50%;
  transform: translateY(-50%);
  width: 3px;
  height: 0;
  background: linear-gradient(to bottom, #667eea, #764ba2);
  transition: height 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  border-radius: 2px;
}

.menu-item:hover {
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.05) 100%);
  color: #667eea;
  transform: translateX(4px);
  box-shadow: 0 2px 6px rgba(102, 126, 234, 0.1);
}

.menu-item:hover::before {
  height: 50%;
}

.menu-item.router-link-active {
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.15) 0%, rgba(118, 75, 162, 0.1) 100%);
  color: #667eea;
  font-weight: 600;
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.2);
}

.menu-item.router-link-active::before {
  height: 70%;
}

.menu-icon {
  font-size: 18px;
  min-width: 24px;
  text-align: center;
  transition: transform 0.3s;
}

.menu-item:hover .menu-icon {
  transform: scale(1.1);
}

.menu-text {
  transition: opacity 0.3s, width 0.3s;
  white-space: nowrap;
}

/* 折叠/展开过渡动画 */
.slide-fade-enter-active,
.slide-fade-leave-active {
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  overflow: hidden;
}

.slide-fade-enter-from {
  opacity: 0;
  max-height: 0;
  transform: translateY(-10px);
}

.slide-fade-enter-to {
  opacity: 1;
  max-height: 500px;
  transform: translateY(0);
}

.slide-fade-leave-from {
  opacity: 1;
  max-height: 500px;
  transform: translateY(0);
}

.slide-fade-leave-to {
  opacity: 0;
  max-height: 0;
  transform: translateY(-10px);
}

/* 主内容区 */
.content {
  flex: 1;
  overflow-y: auto;
  padding: 24px;
}

/* 未读红点 */
.badge {
  background: #e74c3c;
  color: white;
  font-size: 11px;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: 10px;
  min-width: 18px;
  text-align: center;
  line-height: 14px;
  margin-left: auto;
}

/* 子菜单未读红点 */
.sub-badge {
  background: #e74c3c;
  color: white;
  font-size: 10px;
  font-weight: 700;
  padding: 1px 5px;
  border-radius: 8px;
  min-width: 16px;
  text-align: center;
  line-height: 13px;
  margin-left: auto;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .sidebar {
    position: fixed;
    left: 0;
    top: 60px;
    bottom: 0;
    z-index: 50;
    transform: translateX(0);
  }

  .sidebar.collapsed {
    transform: translateX(-100%);
    width: 240px;
  }

  .content {
    margin-left: 0;
  }

  .welcome {
    display: none;
  }
}
</style>
