<template>
  <div class="approval-container">
    <div class="page-header">
      <h2>审批管理</h2>
      <div class="tabs">
        <button :class="{ active: activeTab === 'contract' }" @click="activeTab = 'contract'">📄 合同审批</button>
        <button :class="{ active: activeTab === 'amount' }" @click="activeTab = 'amount'">💰 金额审批</button>
      </div>
    </div>

    <!-- 合同审批 -->
    <div v-if="activeTab === 'contract'" class="approval-card">
      <div v-if="loading" class="loading">加载中...</div>
      <div v-else-if="pendingContracts.length === 0" class="empty">暂无待审核合同</div>
      <table v-else class="approval-table">
        <thead><tr><th>合同名称</th><th>编号</th><th>金额</th><th>创建时间</th><th>操作</th></tr></thead>
        <tbody>
          <tr v-for="c in pendingContracts" :key="c.id">
            <td>{{ c.name }}</td>
            <td>{{ c.contractNumber }}</td>
            <td class="amount">¥{{ c.totalAmount.toFixed(2) }}</td>
            <td>{{ new Date(c.createdAt).toLocaleString() }}</td>
            <td class="actions">
              <button class="btn-approve" @click="approveContract(c.id)">✓ 通过</button>
              <button class="btn-reject" @click="showReject('contract', c.id)">✗ 拒绝</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- 金额审批 -->
    <div v-if="activeTab === 'amount'" class="approval-card">
      <div v-if="loading" class="loading">加载中...</div>
      <div v-else-if="pendingAmounts.length === 0" class="empty">暂无待审核金额</div>
      <table v-else class="approval-table">
        <thead><tr><th>合同名称</th><th>编号</th><th>原金额</th><th>提交金额</th><th>操作</th></tr></thead>
        <tbody>
          <tr v-for="c in pendingAmounts" :key="c.id">
            <td>{{ c.name }}</td>
            <td>{{ c.contractNumber }}</td>
            <td>¥{{ c.totalAmount.toFixed(2) }}</td>
            <td class="amount">¥{{ c.submittedAmount.toFixed(2) }}</td>
            <td class="actions">
              <button class="btn-approve" @click="approveAmount(c.id)">✓ 通过</button>
              <button class="btn-reject" @click="showReject('amount', c.id)">✗ 拒绝</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- 拒绝弹窗 -->
    <div v-if="rejectDialog" class="modal-overlay" @click="rejectDialog = false">
      <div class="modal" @click.stop>
        <h3>拒绝原因</h3>
        <textarea v-model="rejectReason" placeholder="请输入拒绝原因（可选）"></textarea>
        <div class="modal-actions">
          <button @click="confirmReject" class="btn-reject">确认拒绝</button>
          <button @click="rejectDialog = false" class="btn-cancel">取消</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import apiClient from '../api/axios'

const activeTab = ref('contract')
const pendingContracts = ref([])
const pendingAmounts = ref([])
const loading = ref(false)
const rejectDialog = ref(false)
const rejectReason = ref('')
const rejectType = ref('')
const rejectId = ref(null)

async function fetchData() {
  loading.value = true
  try {
    if (activeTab.value === 'contract') {
      const res = await apiClient.get('/contracts/pending-contracts')
      pendingContracts.value = res.data
    } else {
      const res = await apiClient.get('/contracts/pending-amounts')
      pendingAmounts.value = res.data
    }
  } catch (e) { console.error(e) }
  finally { loading.value = false }
}

watch(activeTab, fetchData)
onMounted(fetchData)

async function approveContract(id) {
  await apiClient.post(`/contracts/${id}/approve-contract`)
  await fetchData()
}

async function approveAmount(id) {
  await apiClient.post(`/contracts/${id}/approve-amount`)
  await fetchData()
}

function showReject(type, id) {
  rejectType.value = type
  rejectId.value = id
  rejectReason.value = ''
  rejectDialog.value = true
}

async function confirmReject() {
  const endpoint = rejectType.value === 'contract' ? 'reject-contract' : 'reject-amount'
  await apiClient.post(`/contracts/${rejectId.value}/${endpoint}`, { reason: rejectReason.value })
  rejectDialog.value = false
  await fetchData()
}
</script>

<style scoped>
.approval-container { max-width: 1000px; margin: 0 auto; }
.page-header { margin-bottom: 24px; }
.page-header h2 { color: #2c3e50; font-size: 28px; font-weight: 600; margin-bottom: 16px; }
.tabs { display: flex; gap: 8px; }
.tabs button { padding: 10px 24px; border: 2px solid #e0e0e0; border-radius: 8px; background: white; cursor: pointer; font-size: 14px; font-weight: 600; transition: all 0.3s; }
.tabs button.active { border-color: #667eea; background: linear-gradient(135deg, #667eea, #764ba2); color: white; }
.approval-card { background: white; border-radius: 12px; padding: 24px; box-shadow: 0 2px 12px rgba(0,0,0,0.08); }
.loading, .empty { text-align: center; padding: 40px; color: #95a5a6; }
.approval-table { width: 100%; border-collapse: collapse; }
.approval-table th, .approval-table td { padding: 14px; text-align: left; border-bottom: 1px solid #f0f0f0; }
.approval-table th { background: #f8f9fa; color: #2c3e50; font-weight: 600; }
.amount { color: #667eea; font-weight: 600; }
.actions { display: flex; gap: 8px; }
.btn-approve { padding: 8px 14px; border: none; border-radius: 6px; background: #27ae60; color: white; cursor: pointer; font-weight: 600; }
.btn-approve:hover { background: #229954; }
.btn-reject { padding: 8px 14px; border: none; border-radius: 6px; background: #e74c3c; color: white; cursor: pointer; font-weight: 600; }
.btn-reject:hover { background: #c0392b; }
.modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; }
.modal { background: white; border-radius: 12px; padding: 32px; max-width: 500px; width: 90%; }
.modal h3 { margin-bottom: 16px; }
.modal textarea { width: 100%; min-height: 80px; padding: 12px; border: 2px solid #e0e0e0; border-radius: 8px; font-family: inherit; margin-bottom: 16px; }
.modal-actions { display: flex; gap: 12px; justify-content: flex-end; }
.btn-cancel { padding: 10px 24px; border: none; border-radius: 6px; background: #ecf0f1; color: #34495e; cursor: pointer; font-weight: 600; }
</style>
