<template>
  <div>
    <router-view></router-view>
    <!-- 空闲超时弹窗 -->
    <div v-if="showIdleModal" class="idle-overlay">
      <div class="idle-modal">
        <div class="idle-icon">⏰</div>
        <h3>会话已过期</h3>
        <p>您已超过30分钟未操作，请重新登录。</p>
        <button @click="handleIdleLogout" class="idle-btn">重新登录</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const showIdleModal = ref(false)

const IDLE_TIMEOUT = 30 * 60 * 1000 // 30分钟
const CHECK_INTERVAL = 60 * 1000 // 每60秒检查一次

let lastActivity = Date.now()
let checkTimer = null

const activityEvents = ['mousemove', 'click', 'keypress', 'scroll', 'touchstart']

function updateActivity() {
  lastActivity = Date.now()
}

function checkIdle() {
  if (!authStore.isAuthenticated) return
  if (Date.now() - lastActivity > IDLE_TIMEOUT) {
    showIdleModal.value = true
  }
}

function handleIdleLogout() {
  showIdleModal.value = false
  authStore.logout()
  router.push('/login')
}

function startIdleDetection() {
  activityEvents.forEach(event => {
    document.addEventListener(event, updateActivity, { passive: true })
  })
  checkTimer = setInterval(checkIdle, CHECK_INTERVAL)
}

function stopIdleDetection() {
  activityEvents.forEach(event => {
    document.removeEventListener(event, updateActivity)
  })
  if (checkTimer) {
    clearInterval(checkTimer)
    checkTimer = null
  }
}

// 监听登录状态变化
watch(() => authStore.isAuthenticated, (isAuth) => {
  if (isAuth) {
    lastActivity = Date.now()
    startIdleDetection()
  } else {
    stopIdleDetection()
    showIdleModal.value = false
  }
})

onMounted(() => {
  if (authStore.isAuthenticated) {
    startIdleDetection()
  }
})

onUnmounted(() => {
  stopIdleDetection()
})
</script>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
}

/* 空闲超时弹窗样式 */
.idle-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10000;
  animation: fadeIn 0.3s ease;
}

.idle-modal {
  background: white;
  border-radius: 16px;
  padding: 40px;
  text-align: center;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  max-width: 400px;
  width: 90%;
  animation: scaleIn 0.3s ease;
}

.idle-icon {
  font-size: 48px;
  margin-bottom: 16px;
}

.idle-modal h3 {
  font-size: 22px;
  color: #2c3e50;
  margin-bottom: 12px;
}

.idle-modal p {
  font-size: 15px;
  color: #7f8c8d;
  margin-bottom: 24px;
  line-height: 1.5;
}

.idle-btn {
  padding: 12px 36px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
}

.idle-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 16px rgba(102, 126, 234, 0.4);
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes scaleIn {
  from { transform: scale(0.9); opacity: 0; }
  to { transform: scale(1); opacity: 1; }
}
</style>
