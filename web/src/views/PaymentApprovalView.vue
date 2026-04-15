<template>
  <div class="payment-approval-view">
    <div class="page-header">
      <h2>💳 支付审批</h2>
    </div>

    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="payments.length === 0" class="empty">
      <div class="empty-icon">✅</div>
      <p>暂无待审批的支付记录</p>
    </div>

    <div v-else class="payments-list">
      <div v-for="payment in payments" :key="payment.id" class="payment-card">
        <div class="card-header">
          <div class="contract-info">
            <span class="contract-name">{{ payment.contractName }}</span>
            <span class="contract-number">{{ payment.contractNumber || '-' }}</span>
          </div>
          <span class="pending-badge">⏳ 待审批</span>
        </div>

        <div class="card-body">
          <div class="info-grid">
            <div class="info-item">
              <span class="label">支付金额</span>
              <span class="value amount">¥{{ formatAmount(payment.amount) }}</span>
            </div>
            <div class="info-item">
              <span class="label">支付日期</span>
              <span class="value">{{ formatDate(payment.paymentDate) }}</span>
            </div>
            <div class="info-item">
              <span class="label">合同总额</span>
              <span class="value">¥{{ formatAmount(payment.contractTotalAmount) }}</span>
            </div>
            <div class="info-item">
              <span class="label">已支付</span>
              <span class="value">¥{{ formatAmount(payment.contractPaidAmount) }}</span>
            </div>
          </div>
          <div v-if="payment.note" class="note-section">
            <span class="label">备注：</span>
            <span class="note-text">{{ payment.note }}</span>
          </div>
          <div class="submitter-info">
            <span>提交人：{{ payment.createdByName || '未知' }}</span>
            <span>提交时间：{{ formatDateTime(payment.createdAt) }}</span>
          </div>
        </div>

        <div class="card-actions">
          <button @click="handleApprove(payment.id)" class="approve-btn" :disabled="processing === payment.id">
            ✅ 通过
          </button>
          <button @click="openRejectDialog(payment)" class="reject-btn" :disabled="processing === payment.id">
            ❌ 驳回
          </button>
        </div>
      </div>
    </div>

    <!-- 驳回弹窗 -->
    <div v-if="rejectDialog.show" class="modal-overlay" @click.self="rejectDialog.show = false">
      <div class="modal">
        <h3>驳回支付</h3>
        <p>请输入驳回原因：</p>
        <textarea v-model="rejectDialog.reason" rows="3" placeholder="请输入驳回原因（选填）"></textarea>
        <div class="modal-actions">
          <button @click="handleReject" class="confirm-reject-btn" :disabled="processing">确认驳回</button>
          <button @click="rejectDialog.show = false" class="cancel-btn">取消</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from '../api/axios'

const payments = ref([])
const loading = ref(false)
const error = ref(null)
const processing = ref(null)

const rejectDialog = ref({ show: false, paymentId: null, reason: '' })

async function fetchPendingPayments() {
  loading.value = true; error.value = null
  try {
    const res = await axios.get('/contracts/pending-payments')
    payments.value = res.data
  } catch (err) {
    error.value = '加载失败：' + (err.response?.data?.message || err.message)
  } finally { loading.value = false }
}

async function handleApprove(id) {
  if (!confirm('确定通过该支付记录？')) return
  processing.value = id
  try {
    await axios.post(`/contracts/payments/${id}/approve`)
    payments.value = payments.value.filter(p => p.id !== id)
    alert('已通过')
  } catch (err) { alert('操作失败：' + (err.response?.data?.message || err.message)) }
  finally { processing.value = null }
}

function openRejectDialog(payment) {
  rejectDialog.value = { show: true, paymentId: payment.id, reason: '' }
}

async function handleReject() {
  processing.value = rejectDialog.value.paymentId
  try {
    await axios.post(`/contracts/payments/${rejectDialog.value.paymentId}/reject`, { reason: rejectDialog.value.reason })
    payments.value = payments.value.filter(p => p.id !== rejectDialog.value.paymentId)
    rejectDialog.value.show = false
    alert('已驳回')
  } catch (err) { alert('操作失败：' + (err.response?.data?.message || err.message)) }
  finally { processing.value = null }
}

function formatAmount(a) { return (a || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function formatDate(d) { return d ? new Date(d).toLocaleDateString('zh-CN') : '-' }
function formatDateTime(d) { return d ? new Date(d).toLocaleString('zh-CN') : '-' }

onMounted(() => { fetchPendingPayments() })
</script>

<style scoped>
.payment-approval-view { max-width: 900px; margin: 0 auto; }
.page-header { margin-bottom: 24px; }
.page-header h2 { margin: 0; font-size: 28px; color: #2c3e50; }
.loading, .error, .empty { text-align: center; padding: 60px 20px; font-size: 16px; color: #7f8c8d; background: white; border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); }
.error { color: #e74c3c; }
.empty-icon { font-size: 64px; margin-bottom: 12px; }
.payments-list { display: flex; flex-direction: column; gap: 20px; }
.payment-card { background: white; border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); overflow: hidden; }
.card-header { display: flex; justify-content: space-between; align-items: center; padding: 16px 24px; background: linear-gradient(135deg, rgba(102,126,234,0.08), rgba(118,75,162,0.05)); border-bottom: 1px solid #ecf0f1; }
.contract-info { display: flex; flex-direction: column; gap: 4px; }
.contract-name { font-weight: 600; color: #2c3e50; font-size: 16px; }
.contract-number { font-size: 13px; color: #7f8c8d; font-family: monospace; }
.pending-badge { padding: 6px 14px; background: #fff3cd; color: #856404; border-radius: 16px; font-size: 13px; font-weight: 600; }
.card-body { padding: 20px 24px; }
.info-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 16px; margin-bottom: 16px; }
.info-item { display: flex; flex-direction: column; gap: 4px; }
.info-item .label { font-size: 12px; color: #95a5a6; font-weight: 600; text-transform: uppercase; }
.info-item .value { font-size: 15px; color: #2c3e50; font-weight: 500; }
.info-item .value.amount { font-size: 20px; font-weight: 700; color: #e67e22; }
.note-section { padding: 10px 14px; background: #f8f9fa; border-radius: 6px; margin-bottom: 12px; font-size: 14px; color: #5a6c7d; }
.note-section .label { font-weight: 600; color: #7f8c8d; }
.submitter-info { display: flex; gap: 24px; font-size: 13px; color: #95a5a6; }
.card-actions { display: flex; gap: 12px; padding: 16px 24px; border-top: 1px solid #ecf0f1; background: #fafbfc; }
.approve-btn, .reject-btn { flex: 1; padding: 12px; border: none; border-radius: 8px; font-size: 15px; font-weight: 600; cursor: pointer; transition: all 0.3s; }
.approve-btn { background: #27ae60; color: white; }
.approve-btn:hover:not(:disabled) { background: #229954; transform: translateY(-1px); }
.reject-btn { background: #e74c3c; color: white; }
.reject-btn:hover:not(:disabled) { background: #c0392b; transform: translateY(-1px); }
.approve-btn:disabled, .reject-btn:disabled { opacity: 0.5; cursor: not-allowed; }
/* 弹窗 */
.modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; }
.modal { background: white; border-radius: 12px; padding: 28px; width: 420px; max-width: 90vw; }
.modal h3 { margin: 0 0 12px; color: #2c3e50; }
.modal p { margin: 0 0 12px; color: #7f8c8d; }
.modal textarea { width: 100%; padding: 10px; border: 2px solid #ecf0f1; border-radius: 6px; font-size: 14px; resize: vertical; font-family: inherit; box-sizing: border-box; }
.modal textarea:focus { outline: none; border-color: #667eea; }
.modal-actions { display: flex; gap: 12px; margin-top: 16px; }
.confirm-reject-btn { flex: 1; padding: 10px; background: #e74c3c; color: white; border: none; border-radius: 6px; font-weight: 600; cursor: pointer; }
.cancel-btn { flex: 1; padding: 10px; background: #ecf0f1; color: #2c3e50; border: none; border-radius: 6px; font-weight: 600; cursor: pointer; }
@media (max-width: 768px) { .info-grid { grid-template-columns: 1fr; } .submitter-info { flex-direction: column; gap: 4px; } }
</style>
