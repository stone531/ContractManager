<template>
  <div class="dashboard">
    <!-- 欢迎区域 -->
    <div class="welcome-section">
      <div class="welcome-content">
        <h1 class="welcome-title">
          <span class="wave">👋</span> 欢迎, {{ authStore.user?.name }}
        </h1>
        <p class="welcome-subtitle">合同管理系统 - 今日概览</p>
        <div class="login-info">
          <span class="info-item">
            <span class="info-icon">🕐</span>
            {{ currentTime }}
          </span>
          <span class="info-item">
            <span class="info-icon">🔑</span>
            {{ isSuperAdmin ? '超级管理员' : '普通用户' }}
          </span>
        </div>
      </div>
      <div class="welcome-illustration">
        <div class="illustration-circle"></div>
        <div class="illustration-icon">📊</div>
      </div>
    </div>

    <!-- 今日统计卡片 -->
    <div class="stats-section">
      <div class="stat-card" @click="$router.push('/contracts/list')">
        <div class="stat-icon" style="background: linear-gradient(135deg, #667eea, #764ba2);">📄</div>
        <div class="stat-info">
          <span class="stat-value">{{ stats.todayNewContracts }}</span>
          <span class="stat-label">今日新增合同</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon" style="background: linear-gradient(135deg, #f093fb, #f5576c);">💰</div>
        <div class="stat-info">
          <span class="stat-value">¥{{ formatCompact(stats.todayNewAmount) }}</span>
          <span class="stat-label">今日新增合同金额</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon" style="background: linear-gradient(135deg, #4facfe, #00f2fe);">💳</div>
        <div class="stat-info">
          <span class="stat-value">¥{{ formatCompact(stats.todayPaidAmount) }}</span>
          <span class="stat-label">今日收到付款</span>
        </div>
      </div>
      <div class="stat-card" v-if="isSuperAdmin">
        <div class="stat-icon" style="background: linear-gradient(135deg, #fa709a, #fee140);">⏳</div>
        <div class="stat-info">
          <span class="stat-value">{{ stats.todayPendingApprovals }}</span>
          <span class="stat-label">待审批</span>
        </div>
      </div>
      <div class="stat-card" v-if="!isSuperAdmin">
        <div class="stat-icon" style="background: linear-gradient(135deg, #a18cd1, #fbc2eb);">🔔</div>
        <div class="stat-info">
          <span class="stat-value">{{ stats.unreadNotifications }}</span>
          <span class="stat-label">未读通知</span>
        </div>
      </div>
    </div>

    <!-- 合同状态概览 -->
    <div class="overview-section">
      <div class="overview-card">
        <h3 class="card-title">📊 合同状态分布 <span class="total-hint">（共 {{ stats.totalContracts }} 份）</span></h3>
        <div class="status-bars">
          <div class="status-bar-item" v-for="item in statusDistribution" :key="item.label">
            <div class="bar-label">
              <span class="bar-dot" :style="{ background: item.color }"></span>
              <span>{{ item.label }}</span>
              <span class="bar-count">{{ item.count }}</span>
            </div>
            <div class="bar-track">
              <div class="bar-fill" :style="{ width: item.percent + '%', background: item.color }"></div>
            </div>
          </div>
        </div>
      </div>

      <div class="overview-card">
        <h3 class="card-title">🚀 快捷操作</h3>
        <div class="quick-actions-grid">
          <router-link to="/contracts/add" class="quick-action">
            <span class="qa-icon">➕</span>
            <span>新增合同</span>
          </router-link>
          <router-link to="/contracts/list" class="quick-action">
            <span class="qa-icon">📋</span>
            <span>合同列表</span>
          </router-link>
          <router-link v-if="isSuperAdmin" to="/approval/contracts" class="quick-action">
            <span class="qa-icon">✅</span>
            <span>合同审批</span>
          </router-link>
          <router-link v-if="isSuperAdmin" to="/approval/payments" class="quick-action">
            <span class="qa-icon">💳</span>
            <span>支付审批</span>
          </router-link>
          <router-link v-if="isSuperAdmin" to="/users/list" class="quick-action">
            <span class="qa-icon">👥</span>
            <span>用户管理</span>
          </router-link>
          <router-link to="/notifications/contract" class="quick-action">
            <span class="qa-icon">🔔</span>
            <span>我的通知</span>
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useAuthStore } from '../stores/auth'
import apiClient from '../api/axios'

const authStore = useAuthStore()
const currentTime = ref('')

const isSuperAdmin = computed(() => {
  const role = authStore.user?.role
  return role === 0 || role === 'SuperAdmin'
})

const stats = ref({
  todayNewContracts: 0,
  todayNewAmount: 0,
  todayPaidAmount: 0,
  todayPendingApprovals: 0,
  unreadNotifications: 0,
  totalContracts: 0,
  byStatus: { 0: 0, 1: 0, 2: 0, 3: 0 }
})

const statusDistribution = computed(() => {
  const s = stats.value.byStatus
  const total = stats.value.totalContracts || 1
  return [
    { label: '初始', count: s[0] || 0, color: '#95a5a6', percent: ((s[0] || 0) / total) * 100 },
    { label: '进行中', count: s[1] || 0, color: '#3498db', percent: ((s[1] || 0) / total) * 100 },
    { label: '已完成', count: s[2] || 0, color: '#27ae60', percent: ((s[2] || 0) / total) * 100 },
    { label: '已终止', count: s[3] || 0, color: '#e74c3c', percent: ((s[3] || 0) / total) * 100 }
  ]
})

function formatCompact(num) {
  if (num >= 100000000) return (num / 100000000).toFixed(1) + '亿'
  if (num >= 10000) return (num / 10000).toFixed(1) + '万'
  return (num || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

async function loadDashboardData() {
  try {
    const res = await apiClient.get('/contracts/today-stats')
    const data = res.data
    stats.value.todayNewContracts = data.todayNewContracts || 0
    stats.value.todayNewAmount = data.todayNewAmount || 0
    stats.value.todayPaidAmount = data.todayPaidAmount || 0
    stats.value.todayPendingApprovals = data.todayPendingApprovals || 0
    stats.value.unreadNotifications = data.unreadNotifications || 0
    stats.value.totalContracts = data.totalContracts || 0
    stats.value.byStatus = data.byStatus || { 0: 0, 1: 0, 2: 0, 3: 0 }
  } catch (e) { /* ignore */ }
}

function formatTime() {
  const now = new Date()
  currentTime.value = now.toLocaleString('zh-CN', {
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit'
  })
}

onMounted(() => {
  formatTime()
  setInterval(formatTime, 1000)
  loadDashboardData()
})
</script>

<style scoped>
.dashboard { max-width: 1200px; margin: 0 auto; }

/* 欢迎区域 */
.welcome-section {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 16px; padding: 40px 48px; margin-bottom: 28px;
  display: flex; justify-content: space-between; align-items: center;
  box-shadow: 0 8px 24px rgba(102,126,234,0.25); position: relative; overflow: hidden;
}
.welcome-section::before {
  content: ''; position: absolute; top: -50%; right: -10%;
  width: 400px; height: 400px;
  background: radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%); border-radius: 50%;
}
.welcome-content { flex: 1; color: white; position: relative; z-index: 1; }
.welcome-title { font-size: 32px; font-weight: 700; margin: 0 0 8px 0; display: flex; align-items: center; gap: 12px; }
.wave { display: inline-block; animation: wave 2s ease-in-out infinite; transform-origin: 70% 70%; font-size: 38px; }
@keyframes wave { 0%,100% { transform: rotate(0deg); } 10%,30% { transform: rotate(14deg); } 20% { transform: rotate(-8deg); } }
.welcome-subtitle { font-size: 16px; margin: 0 0 16px 0; opacity: 0.9; }
.login-info { display: flex; gap: 16px; flex-wrap: wrap; }
.info-item { display: flex; align-items: center; gap: 6px; font-size: 13px; background: rgba(255,255,255,0.15); padding: 6px 14px; border-radius: 18px; }
.welcome-illustration { position: relative; width: 120px; height: 120px; display: flex; align-items: center; justify-content: center; }
.illustration-circle { position: absolute; width: 100%; height: 100%; background: rgba(255,255,255,0.15); border-radius: 50%; animation: pulse 2s ease-in-out infinite; }
@keyframes pulse { 0%,100% { transform: scale(1); opacity: 0.8; } 50% { transform: scale(1.1); opacity: 0.4; } }
.illustration-icon { font-size: 64px; animation: float 3s ease-in-out infinite; }
@keyframes float { 0%,100% { transform: translateY(0); } 50% { transform: translateY(-8px); } }

/* 统计卡片 */
.stats-section { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 20px; margin-bottom: 28px; }
.stat-card {
  background: white; border-radius: 14px; padding: 24px;
  display: flex; align-items: center; gap: 16px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.06); cursor: pointer;
  transition: all 0.3s;
}
.stat-card:hover { transform: translateY(-4px); box-shadow: 0 8px 24px rgba(0,0,0,0.1); }
.stat-icon { width: 56px; height: 56px; border-radius: 14px; display: flex; align-items: center; justify-content: center; font-size: 28px; flex-shrink: 0; }
.stat-info { display: flex; flex-direction: column; gap: 4px; }
.stat-value { font-size: 22px; font-weight: 700; color: #2c3e50; }
.stat-label { font-size: 13px; color: #95a5a6; }

/* 概览区 */
.overview-section { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 28px; }
.overview-card { background: white; border-radius: 14px; padding: 24px; box-shadow: 0 2px 12px rgba(0,0,0,0.06); }
.card-title { margin: 0 0 20px; font-size: 18px; color: #2c3e50; }
.total-hint { font-size: 13px; color: #95a5a6; font-weight: 400; }

/* 状态分布 */
.status-bars { display: flex; flex-direction: column; gap: 16px; }
.status-bar-item {}
.bar-label { display: flex; align-items: center; gap: 8px; margin-bottom: 6px; font-size: 14px; color: #5a6c7d; }
.bar-dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }
.bar-count { margin-left: auto; font-weight: 700; color: #2c3e50; }
.bar-track { height: 8px; background: #f0f2f5; border-radius: 4px; overflow: hidden; }
.bar-fill { height: 100%; border-radius: 4px; transition: width 0.6s ease; min-width: 2px; }

/* 快捷操作 */
.quick-actions-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; }
.quick-action {
  display: flex; flex-direction: column; align-items: center; gap: 8px;
  padding: 18px 12px; border-radius: 12px; text-decoration: none; color: #5a6c7d;
  background: #f8f9fb; transition: all 0.3s; font-size: 13px; font-weight: 500;
}
.quick-action:hover { background: linear-gradient(135deg, rgba(102,126,234,0.1), rgba(118,75,162,0.05)); color: #667eea; transform: translateY(-2px); }
.qa-icon { font-size: 28px; }

@media (max-width: 768px) {
  .welcome-section { padding: 28px 20px; flex-direction: column; text-align: center; }
  .welcome-title { font-size: 24px; justify-content: center; }
  .login-info { justify-content: center; }
  .welcome-illustration { margin-top: 16px; width: 80px; height: 80px; }
  .illustration-icon { font-size: 48px; }
  .stats-section { grid-template-columns: repeat(2, 1fr); }
  .overview-section { grid-template-columns: 1fr; }
  .quick-actions-grid { grid-template-columns: repeat(2, 1fr); }
}
</style>
