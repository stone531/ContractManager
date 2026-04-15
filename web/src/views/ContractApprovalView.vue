<template>
  <div class="approval-view">
    <h2>📋 合同审批</h2>
    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="contracts.length === 0" class="empty">暂无待审核合同</div>
    <div v-else class="list">
      <div v-for="c in contracts" :key="c.id" class="card">
        <div class="card-info">
          <div class="card-title">{{ c.name }}</div>
          <div class="card-meta">编号: {{ c.contractNumber }} | 金额: ¥{{ c.totalAmount?.toFixed(2) }}</div>
          <div class="card-meta">创建时间: {{ new Date(c.createdAt).toLocaleString('zh-CN') }}</div>
        </div>
        <div class="card-actions">
          <button class="approve-btn" @click="approve(c.id)">✅ 通过</button>
          <button class="reject-btn" @click="reject(c.id)">❌ 驳回</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from '../api/axios'

const contracts = ref([])
const loading = ref(false)

async function fetch() {
  loading.value = true
  try {
    const res = await axios.get('/contracts/pending-contracts')
    contracts.value = res.data
  } catch (e) { console.error(e) }
  finally { loading.value = false }
}

async function approve(id) {
  if (!confirm('确认通过该合同？')) return
  await axios.post(`/contracts/${id}/approve-contract`)
  fetch()
}

async function reject(id) {
  const reason = prompt('请输入驳回原因（可选）')
  await axios.post(`/contracts/${id}/reject-contract`, { reason })
  fetch()
}

onMounted(fetch)
</script>

<style scoped>
.approval-view { max-width: 900px; margin: 0 auto; }
h2 { margin-bottom: 24px; color: #2c3e50; }
.loading, .empty { text-align: center; padding: 60px; color: #95a5a6; background: white; border-radius: 12px; }
.list { display: flex; flex-direction: column; gap: 16px; }
.card { background: white; border-radius: 12px; padding: 20px; display: flex; justify-content: space-between; align-items: center; box-shadow: 0 2px 8px rgba(0,0,0,.08); }
.card-title { font-size: 18px; font-weight: 600; color: #2c3e50; margin-bottom: 6px; }
.card-meta { font-size: 13px; color: #7f8c8d; }
.card-actions { display: flex; gap: 8px; }
.approve-btn, .reject-btn { padding: 8px 16px; border: none; border-radius: 6px; cursor: pointer; font-weight: 600; font-size: 13px; }
.approve-btn { background: #27ae60; color: white; }
.approve-btn:hover { background: #229954; }
.reject-btn { background: #e74c3c; color: white; }
.reject-btn:hover { background: #c0392b; }
</style>
