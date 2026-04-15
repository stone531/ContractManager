<template>
  <div class="notification-view">
    <div class="header-row">
      <h2>📄 合同消息通知</h2>
      <button v-if="notifications.length" @click="markAllRead" class="read-all-btn">全部已读</button>
    </div>
    <div v-if="notifications.length === 0" class="empty">暂无合同相关通知</div>
    <div v-else class="list">
      <div v-for="n in notifications" :key="n.id" class="noti-card" :class="{ unread: !n.isRead }" @click="markRead(n)">
        <div class="noti-icon">{{ getIcon(n.type) }}</div>
        <div class="noti-content">
          <div class="noti-msg">{{ n.message }}</div>
          <div v-if="n.rejectReason" class="noti-reason">驳回原因: {{ n.rejectReason }}</div>
          <div class="noti-time">{{ new Date(n.createdAt).toLocaleString('zh-CN') }}</div>
        </div>
        <div v-if="!n.isRead" class="dot"></div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, inject } from 'vue'
import axios from '../api/axios'

const notifications = ref([])
const refreshUnreadCount = inject('refreshUnreadCount', () => {})

async function fetch() {
  const res = await axios.get('/notifications', { params: { category: 'contract' } })
  notifications.value = res.data
}
async function markRead(n) {
  if (n.isRead) return
  await axios.post(`/notifications/${n.id}/read`)
  n.isRead = true
  refreshUnreadCount()
}
async function markAllRead() {
  await axios.post('/notifications/read-all')
  notifications.value.forEach(n => n.isRead = true)
  refreshUnreadCount()
}
function getIcon(type) {
  const map = { 0: '📋', 2: '✅', 3: '❌' }
  return map[type] || '🔔'
}
onMounted(fetch)
</script>

<style scoped>
.notification-view { max-width: 800px; margin: 0 auto; }
.header-row { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
h2 { margin: 0; color: #2c3e50; }
.read-all-btn { padding: 8px 16px; background: #667eea; color: white; border: none; border-radius: 6px; cursor: pointer; font-weight: 600; font-size: 13px; }
.empty { text-align: center; padding: 60px; color: #95a5a6; background: white; border-radius: 12px; }
.list { display: flex; flex-direction: column; gap: 10px; }
.noti-card { display: flex; align-items: flex-start; gap: 14px; padding: 16px; background: white; border-radius: 10px; box-shadow: 0 1px 4px rgba(0,0,0,.06); cursor: pointer; transition: all .2s; position: relative; }
.noti-card.unread { background: #f0f4ff; border-left: 4px solid #667eea; }
.noti-card:hover { box-shadow: 0 2px 8px rgba(0,0,0,.12); }
.noti-icon { font-size: 28px; }
.noti-msg { font-size: 14px; color: #2c3e50; margin-bottom: 4px; }
.noti-reason { font-size: 13px; color: #e74c3c; margin-bottom: 4px; }
.noti-time { font-size: 12px; color: #95a5a6; }
.dot { width: 10px; height: 10px; background: #e74c3c; border-radius: 50%; position: absolute; top: 16px; right: 16px; }
</style>
