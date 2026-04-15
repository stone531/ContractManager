<template>
  <div class="approval-view">
    <h2>💰 金额审批</h2>

    <!-- 金额变更审批 -->
    <h3>金额变更</h3>
    <div v-if="amounts.length === 0" class="empty">暂无待审核金额变更</div>
    <div v-else class="list">
      <div v-for="c in amounts" :key="'a'+c.id" class="card">
        <div class="card-info">
          <div class="card-title">{{ c.name }} ({{ c.contractNumber }})</div>
          <div class="card-meta">当前金额: ¥{{ c.totalAmount?.toFixed(2) }} → 申请变更: <strong>¥{{ c.submittedAmount?.toFixed(2) }}</strong></div>
        </div>
        <div class="card-actions">
          <button class="approve-btn" @click="approveAmount(c.id)">✅ 通过</button>
          <button class="reject-btn" @click="rejectAmount(c.id)">❌ 驳回</button>
        </div>
      </div>
    </div>

    <!-- 支付审批 -->
    <h3 style="margin-top:32px">支付审批</h3>
    <div v-if="payments.length === 0" class="empty">暂无待审核支付记录</div>
    <div v-else class="list">
      <div v-for="p in payments" :key="'p'+p.id" class="card">
        <div class="card-info">
          <div class="card-title">{{ p.contractName }} ({{ p.contractNumber }})</div>
          <div class="card-meta">支付金额: <strong>¥{{ p.amount?.toFixed(2) }}</strong> | 支付日期: {{ new Date(p.paymentDate).toLocaleDateString('zh-CN') }}</div>
          <div v-if="p.note" class="card-meta">备注: {{ p.note }}</div>
        </div>
        <div class="card-actions">
          <button class="approve-btn" @click="approvePayment(p.id)">✅ 通过</button>
          <button class="reject-btn" @click="rejectPayment(p.id)">❌ 驳回</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from '../api/axios'

const amounts = ref([])
const payments = ref([])

async function fetchAll() {
  try {
    const [a, p] = await Promise.all([
      axios.get('/contracts/pending-amounts'),
      axios.get('/contracts/pending-payments')
    ])
    amounts.value = a.data
    payments.value = p.data
  } catch (e) { console.error(e) }
}

async function approveAmount(id) {
  if (!confirm('确认通过金额变更？')) return
  await axios.post(`/contracts/${id}/approve-amount`)
  fetchAll()
}
async function rejectAmount(id) {
  const reason = prompt('驳回原因（可选）')
  await axios.post(`/contracts/${id}/reject-amount`, { reason })
  fetchAll()
}
async function approvePayment(id) {
  if (!confirm('确认通过该支付？')) return
  await axios.post(`/contracts/payments/${id}/approve`)
  fetchAll()
}
async function rejectPayment(id) {
  const reason = prompt('驳回原因（可选）')
  await axios.post(`/contracts/payments/${id}/reject`, { reason })
  fetchAll()
}

onMounted(fetchAll)
</script>

<style scoped>
.approval-view { max-width: 900px; margin: 0 auto; }
h2 { margin-bottom: 24px; color: #2c3e50; }
h3 { margin-bottom: 16px; color: #34495e; font-size: 16px; }
.loading, .empty { text-align: center; padding: 40px; color: #95a5a6; background: white; border-radius: 12px; margin-bottom: 16px; }
.list { display: flex; flex-direction: column; gap: 12px; margin-bottom: 16px; }
.card { background: white; border-radius: 12px; padding: 20px; display: flex; justify-content: space-between; align-items: center; box-shadow: 0 2px 8px rgba(0,0,0,.08); }
.card-title { font-size: 16px; font-weight: 600; color: #2c3e50; margin-bottom: 4px; }
.card-meta { font-size: 13px; color: #7f8c8d; }
.card-actions { display: flex; gap: 8px; flex-shrink: 0; }
.approve-btn, .reject-btn { padding: 8px 16px; border: none; border-radius: 6px; cursor: pointer; font-weight: 600; font-size: 13px; }
.approve-btn { background: #27ae60; color: white; }
.reject-btn { background: #e74c3c; color: white; }
</style>
