<template>
  <div class="notification-container">
    <div class="page-header">
      <h2>通知管理</h2>
      <button v-if="notifications.length > 0" @click="markAllRead" class="btn-read-all">全部标记已读</button>
    </div>

    <div class="notification-card">
      <div v-if="loading" class="loading">加载中...</div>
      <div v-else-if="notifications.length === 0" class="empty">暂无通知</div>
      <div v-else class="notification-list">
        <div v-for="n in notifications" :key="n.id" class="notification-item" :class="{ unread: !n.isRead }" @click="markRead(n)">
          <div class="notif-dot" v-if="!n.isRead"></div>
          <div class="notif-content">
            <div class="notif-icon">{{ getIcon(n.type) }}</div>
            <div class="notif-body">
              <p class="notif-message">{{ n.message }}</p>
              <p v-if="n.rejectReason" class="notif-reason">驳回原因：{{ n.rejectReason }}</p>
              <span class="notif-time">{{ new Date(n.createdAt).toLocaleString() }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import apiClient from '../api/axios'

const notifications = ref([])
const loading = ref(false)

function getIcon(type) {
  const icons = { 0: '📄', 1: '💰', 2: '✅', 3: '❌', 4: '✅', 5: '❌' }
  return icons[type] || '📢'
}

async function fetchNotifications() {
  loading.value = true
  try {
    const res = await apiClient.get('/notifications')
    notifications.value = res.data
  } catch (e) { console.error(e) }
  finally { loading.value = false }
}

async function markRead(n) {
  if (!n.isRead) {
    await apiClient.post(`/notifications/${n.id}/read`)
    n.isRead = true
  }
}

async function markAllRead() {
  await apiClient.post('/notifications/read-all')
  notifications.value.forEach(n => n.isRead = true)
}

onMounted(fetchNotifications)
</script>

<style scoped>
.notification-container { max-width: 800px; margin: 0 auto; }
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }
.page-header h2 { color: #2c3e50; font-size: 28px; font-weight: 600; }
.btn-read-all { padding: 8px 16px; background: #667eea; color: white; border: none; border-radius: 6px; cursor: pointer; font-weight: 600; }
.btn-read-all:hover { background: #5a6fd6; }
.notification-card { background: white; border-radius: 12px; padding: 16px; box-shadow: 0 2px 12px rgba(0,0,0,0.08); }
.loading, .empty { text-align: center; padding: 40px; color: #95a5a6; }
.notification-item { display: flex; align-items: flex-start; padding: 16px; border-bottom: 1px solid #f0f0f0; cursor: pointer; transition: background 0.2s; position: relative; }
.notification-item:hover { background: #f8f9fa; }
.notification-item.unread { background: #f0f4ff; }
.notif-dot { position: absolute; left: 8px; top: 24px; width: 8px; height: 8px; background: #e74c3c; border-radius: 50%; }
.notif-content { display: flex; gap: 12px; padding-left: 12px; flex: 1; }
.notif-icon { font-size: 24px; min-width: 32px; }
.notif-body { flex: 1; }
.notif-message { color: #2c3e50; font-size: 14px; margin: 0 0 4px; }
.notif-reason { color: #e74c3c; font-size: 13px; margin: 4px 0; background: #fff5f5; padding: 6px 10px; border-radius: 4px; }
.notif-time { color: #95a5a6; font-size: 12px; }
</style>
