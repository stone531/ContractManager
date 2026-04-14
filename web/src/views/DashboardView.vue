<template>
  <div class="dashboard">
    <!-- 欢迎区域 -->
    <div class="welcome-section">
      <div class="welcome-content">
        <h1 class="welcome-title">
          <span class="wave">👋</span> 欢迎, {{ authStore.user?.name }}
        </h1>
        <p class="welcome-subtitle">您已成功登录用户管理系统</p>
        <div class="login-info">
          <span class="info-item">
            <span class="info-icon">🕐</span>
            登录时间：{{ currentTime }}
          </span>
        </div>
      </div>
      <div class="welcome-illustration">
        <div class="illustration-circle"></div>
        <div class="illustration-icon">🎉</div>
      </div>
    </div>

    <!-- 快捷操作卡片 -->
    <div class="quick-actions">
      <h2 class="section-title">快捷操作</h2>
      <div class="action-cards">
        <router-link to="/users/add" class="action-card add-card">
          <div class="card-icon">📝</div>
          <h3>新增用户</h3>
          <p>添加新的用户到系统</p>
          <div class="card-arrow">→</div>
        </router-link>

        <router-link to="/users/list" class="action-card list-card">
          <div class="card-icon">📋</div>
          <h3>查询用户</h3>
          <p>查看和管理所有用户</p>
          <div class="card-arrow">→</div>
        </router-link>
      </div>
    </div>

    <!-- 系统信息 -->
    <div class="system-info">
      <h2 class="section-title">系统信息</h2>
      <div class="info-cards">
        <div class="info-card">
          <div class="info-card-icon">👤</div>
          <div class="info-card-content">
            <div class="info-card-label">当前用户</div>
            <div class="info-card-value">{{ authStore.user?.name }}</div>
          </div>
        </div>

        <div class="info-card">
          <div class="info-card-icon">📧</div>
          <div class="info-card-content">
            <div class="info-card-label">邮箱地址</div>
            <div class="info-card-value">{{ authStore.user?.email }}</div>
          </div>
        </div>

        <div class="info-card">
          <div class="info-card-icon">🔐</div>
          <div class="info-card-content">
            <div class="info-card-label">登录状态</div>
            <div class="info-card-value status-active">已登录</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useAuthStore } from '../stores/auth'

const authStore = useAuthStore()
const currentTime = ref('')

// 格式化当前时间
function formatTime() {
  const now = new Date()
  currentTime.value = now.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  })
}

onMounted(() => {
  formatTime()
  // 每秒更新时间
  setInterval(formatTime, 1000)
})
</script>

<style scoped>
.dashboard {
  max-width: 1200px;
  margin: 0 auto;
}

/* 欢迎区域 */
.welcome-section {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 16px;
  padding: 48px;
  margin-bottom: 32px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 8px 24px rgba(102, 126, 234, 0.25);
  position: relative;
  overflow: hidden;
}

.welcome-section::before {
  content: '';
  position: absolute;
  top: -50%;
  right: -10%;
  width: 400px;
  height: 400px;
  background: radial-gradient(circle, rgba(255, 255, 255, 0.1) 0%, transparent 70%);
  border-radius: 50%;
}

.welcome-content {
  flex: 1;
  color: white;
  position: relative;
  z-index: 1;
}

.welcome-title {
  font-size: 36px;
  font-weight: 700;
  margin: 0 0 12px 0;
  display: flex;
  align-items: center;
  gap: 12px;
}

.wave {
  display: inline-block;
  animation: wave 2s ease-in-out infinite;
  transform-origin: 70% 70%;
  font-size: 42px;
}

@keyframes wave {
  0%, 100% { transform: rotate(0deg); }
  10%, 30% { transform: rotate(14deg); }
  20% { transform: rotate(-8deg); }
  40% { transform: rotate(-4deg); }
  50% { transform: rotate(10deg); }
}

.welcome-subtitle {
  font-size: 18px;
  margin: 0 0 24px 0;
  opacity: 0.95;
}

.login-info {
  display: flex;
  gap: 24px;
  flex-wrap: wrap;
}

.info-item {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  background: rgba(255, 255, 255, 0.15);
  padding: 8px 16px;
  border-radius: 20px;
  backdrop-filter: blur(10px);
}

.info-icon {
  font-size: 16px;
}

.welcome-illustration {
  position: relative;
  width: 150px;
  height: 150px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.illustration-circle {
  position: absolute;
  width: 100%;
  height: 100%;
  background: rgba(255, 255, 255, 0.15);
  border-radius: 50%;
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { transform: scale(1); opacity: 0.8; }
  50% { transform: scale(1.1); opacity: 0.4; }
}

.illustration-icon {
  font-size: 80px;
  animation: float 3s ease-in-out infinite;
}

@keyframes float {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-10px); }
}

/* 标题 */
.section-title {
  font-size: 20px;
  font-weight: 600;
  color: #2c3e50;
  margin: 0 0 20px 0;
}

/* 快捷操作卡片 */
.quick-actions {
  margin-bottom: 32px;
}

.action-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 24px;
}

.action-card {
  background: white;
  border-radius: 12px;
  padding: 32px;
  text-decoration: none;
  color: inherit;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  position: relative;
  overflow: hidden;
  border: 2px solid transparent;
}

.action-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 4px;
  background: linear-gradient(90deg, #667eea, #764ba2);
  transform: scaleX(0);
  transition: transform 0.3s;
}

.action-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 12px 24px rgba(102, 126, 234, 0.2);
  border-color: #667eea;
}

.action-card:hover::before {
  transform: scaleX(1);
}

.card-icon {
  font-size: 48px;
  margin-bottom: 16px;
  transition: transform 0.3s;
}

.action-card:hover .card-icon {
  transform: scale(1.1) rotate(5deg);
}

.action-card h3 {
  font-size: 20px;
  font-weight: 600;
  margin: 0 0 8px 0;
  color: #2c3e50;
}

.action-card p {
  font-size: 14px;
  color: #7f8c8d;
  margin: 0 0 16px 0;
}

.card-arrow {
  font-size: 24px;
  color: #667eea;
  transition: transform 0.3s;
}

.action-card:hover .card-arrow {
  transform: translateX(8px);
}

/* 系统信息 */
.system-info {
  margin-bottom: 32px;
}

.info-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 20px;
}

.info-card {
  background: white;
  border-radius: 12px;
  padding: 24px;
  display: flex;
  align-items: center;
  gap: 16px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  transition: all 0.3s;
}

.info-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
  transform: translateY(-2px);
}

.info-card-icon {
  font-size: 40px;
  width: 60px;
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  border-radius: 12px;
}

.info-card-content {
  flex: 1;
}

.info-card-label {
  font-size: 12px;
  color: #95a5a6;
  margin-bottom: 4px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.info-card-value {
  font-size: 16px;
  font-weight: 600;
  color: #2c3e50;
}

.status-active {
  color: #27ae60;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .welcome-section {
    padding: 32px 24px;
    flex-direction: column;
    text-align: center;
  }

  .welcome-title {
    font-size: 28px;
    justify-content: center;
  }

  .welcome-subtitle {
    font-size: 16px;
  }

  .login-info {
    justify-content: center;
  }

  .welcome-illustration {
    margin-top: 24px;
    width: 100px;
    height: 100px;
  }

  .illustration-icon {
    font-size: 60px;
  }

  .action-cards {
    grid-template-columns: 1fr;
  }

  .info-cards {
    grid-template-columns: 1fr;
  }
}
</style>
