<template>
  <div class="contract-detail-view">
    <div class="page-header">
      <h2>合同详情</h2>
      <div class="header-actions">
        <button v-if="isSuperAdmin && contract && contract.contractStatus !== 3" @click="handleTerminate" class="terminate-btn">🛑 终止合同</button>
        <router-link to="/contracts/list" class="back-btn">← 返回列表</router-link>
      </div>
    </div>

    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    
    <div v-else-if="contract" class="detail-container">
      <!-- 已终止提示 -->
      <div v-if="contract.contractStatus === 3" class="terminated-banner">🛑 该合同已终止（{{ formatDateTime(contract.terminatedAt) }}），不可进行编辑、支付和金额变更操作</div>

      <!-- 合同信息卡片 -->
      <div class="info-card">
        <div class="card-header">
          <h3>📄 合同信息</h3>
          <div class="badges">
            <span class="status-badge" :class="getStatusClass(contract)">{{ getStatusText(contract) }}</span>
            <span class="contract-status-badge" :class="'cs-' + contract.contractStatus">{{ getContractStatusLabel(contract.contractStatus) }}</span>
          </div>
        </div>
        <div class="info-grid">
          <div class="info-item"><span class="label">合同编号</span><span class="value">{{ contract.contractNumber || '-' }}</span></div>
          <div class="info-item"><span class="label">合同名称</span><span class="value">{{ contract.name }}</span></div>
          <div class="info-item"><span class="label">创建时间</span><span class="value">{{ formatDateTime(contract.createdAt) }}</span></div>
          <div class="info-item"><span class="label">更新时间</span><span class="value">{{ contract.updatedAt ? formatDateTime(contract.updatedAt) : '-' }}</span></div>
          <div class="info-item"><span class="label">生效日期</span><span class="value">{{ contract.startDate ? formatDate(contract.startDate) : '-' }}</span></div>
          <div class="info-item"><span class="label">到期日期</span><span class="value">{{ contract.endDate ? formatDate(contract.endDate) : '-' }}</span></div>
          <div v-if="contract.description" class="info-item full-width"><span class="label">合同描述</span><span class="value">{{ contract.description }}</span></div>
          <div v-if="contract.fileName" class="info-item full-width">
            <span class="label">合同文件</span>
            <div class="file-download">
              <span class="filename">📎 {{ contract.fileName }}</span>
              <button @click="downloadFile" class="download-btn">📥 下载</button>
            </div>
          </div>
        </div>
      </div>

      <!-- 金额统计卡片 -->
      <div class="amount-card">
        <h3>💰 金额统计</h3>
        <div class="amount-summary">
          <div class="amount-box total"><span class="amount-label">合同总额</span><span class="amount-value">¥{{ formatAmount(contract.totalAmount) }}</span></div>
          <div class="amount-box paid"><span class="amount-label">已支付</span><span class="amount-value">¥{{ formatAmount(contract.paidAmount) }}</span></div>
          <div class="amount-box remaining"><span class="amount-label">剩余应付</span><span class="amount-value">¥{{ formatAmount(contract.remainingAmount) }}</span></div>
        </div>
        <!-- 提交金额变更（非超管 + 非终止） -->
        <div v-if="!isSuperAdmin && contract.approvalStatus === 1 && contract.contractStatus !== 3" class="submit-amount-section">
          <div class="submit-amount-header"><h4>📝 提交金额变更</h4><button @click="showAmountForm = !showAmountForm" class="toggle-btn">{{ showAmountForm ? '取消' : '提交金额变更' }}</button></div>
          <div v-if="showAmountForm" class="submit-amount-form">
            <div class="amount-input"><span class="currency">¥</span><input v-model.number="submitAmountValue" type="number" step="0.01" min="0.01" placeholder="请输入新金额" /></div>
            <button @click="handleSubmitAmount" class="submit-btn" :disabled="!submitAmountValue || submittingAmount">{{ submittingAmount ? '提交中...' : '✓ 提交审批' }}</button>
          </div>
          <div v-if="contract.submittedAmount > 0" class="pending-info">⏳ 已提交待审核金额：¥{{ formatAmount(contract.submittedAmount) }}</div>
        </div>
        <div class="progress-section">
          <div class="progress-bar"><div class="progress-fill" :style="{ width: getProgress() + '%' }" :class="getStatusClass(contract)"></div></div>
          <div class="progress-text">完成度: {{ getProgress() }}%</div>
        </div>
      </div>

      <!-- 支付记录 -->
      <div class="payments-card">
        <div class="card-header">
          <h3>💳 支付记录</h3>
          <button v-if="contract.contractStatus !== 3" @click="showAddPayment = !showAddPayment" class="add-payment-btn">{{ showAddPayment ? '✕ 取消' : '➕ 添加支付' }}</button>
        </div>
        <transition name="slide-fade">
          <div v-if="showAddPayment" class="add-payment-form">
            <form @submit.prevent="handleAddPayment">
              <div class="form-row">
                <div class="form-group"><label>支付金额 *</label><div class="amount-input"><span class="currency">¥</span><input v-model.number="paymentForm.amount" type="number" step="0.01" min="0.01" :max="contract.remainingAmount" required placeholder="0.00" /></div><span class="hint">剩余可支付: ¥{{ formatAmount(contract.remainingAmount) }}</span></div>
                <div class="form-group"><label>支付日期 *</label><input v-model="paymentForm.paymentDate" type="date" required /></div>
              </div>
              <div class="form-group"><label>备注说明</label><textarea v-model="paymentForm.note" rows="2" placeholder="请输入备注（选填）"></textarea></div>
              <div class="form-actions"><button type="submit" class="submit-btn" :disabled="submitting">{{ submitting ? '提交中...' : '✓ 确认添加' }}</button></div>
              <div v-if="paymentError" class="error-message">{{ paymentError }}</div>
            </form>
          </div>
        </transition>
        <div v-if="payments.length === 0" class="empty-payments">暂无支付记录</div>
        <div v-else class="payments-list">
          <div v-for="payment in payments" :key="payment.id" class="payment-item" :class="'ps-' + payment.status">
            <div class="payment-icon">💵</div>
            <div class="payment-info">
              <div class="payment-amount">¥{{ formatAmount(payment.amount) }}</div>
              <div class="payment-date">{{ formatDate(payment.paymentDate) }}</div>
              <div v-if="payment.note" class="payment-note">{{ payment.note }}</div>
            </div>
            <div class="payment-status-col">
              <span class="pay-status-badge" :class="'pst-' + payment.status">{{ getPaymentStatusText(payment.status) }}</span>
            </div>
            <div class="payment-time">{{ formatDateTime(payment.createdAt) }}</div>
          </div>
        </div>
      </div>

      <!-- 操作日志 -->
      <div class="logs-card">
        <div class="card-header"><h3>📋 操作日志</h3></div>
        <div v-if="logs.length === 0" class="empty-payments">暂无操作日志</div>
        <div v-else class="timeline">
          <div v-for="log in logs" :key="log.id" class="timeline-item">
            <div class="timeline-dot" :class="getLogDotClass(log.action)"></div>
            <div class="timeline-content">
              <div class="timeline-header">
                <span class="log-action">{{ getLogActionLabel(log.action) }}</span>
                <span class="log-time">{{ formatDateTime(log.createdAt) }}</span>
              </div>
              <div class="log-desc">{{ log.description }}</div>
              <div class="log-user">操作人：{{ log.userName }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from '../api/axios'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const isSuperAdmin = computed(() => { const r = authStore.user?.role; return r === 0 || r === 'SuperAdmin' })

const contract = ref(null)
const payments = ref([])
const logs = ref([])
const loading = ref(false)
const error = ref(null)
const showAddPayment = ref(false)
const submitting = ref(false)
const paymentError = ref(null)
const showAmountForm = ref(false)
const submitAmountValue = ref(null)
const submittingAmount = ref(false)

const paymentForm = ref({ amount: 0, paymentDate: new Date().toISOString().split('T')[0], note: '' })

async function fetchContract() {
  loading.value = true; error.value = null
  try {
    const id = route.params.id
    const [res, logRes] = await Promise.all([axios.get(`/contracts/${id}`), axios.get(`/contracts/${id}/logs`)])
    contract.value = res.data; payments.value = res.data.payments || []; logs.value = logRes.data
  } catch (err) { error.value = '加载失败：' + (err.response?.data?.message || err.message) }
  finally { loading.value = false }
}

async function handleSubmitAmount() {
  submittingAmount.value = true
  try { await axios.post(`/contracts/${contract.value.id}/submit-amount`, { amount: submitAmountValue.value }); alert('金额已提交审批'); showAmountForm.value = false; submitAmountValue.value = null; await fetchContract() }
  catch (err) { alert('提交失败：' + (err.response?.data?.message || err.message)) }
  finally { submittingAmount.value = false }
}

async function handleAddPayment() {
  paymentError.value = null; submitting.value = true
  try { await axios.post(`/contracts/${contract.value.id}/payments`, paymentForm.value); await fetchContract(); paymentForm.value = { amount: 0, paymentDate: new Date().toISOString().split('T')[0], note: '' }; showAddPayment.value = false; alert('支付记录添加成功！') }
  catch (err) { paymentError.value = '添加失败：' + (err.response?.data || err.message) }
  finally { submitting.value = false }
}

async function handleTerminate() {
  if (!confirm('确定要终止该合同吗？终止后不可编辑、不可支付。')) return
  try { await axios.post(`/contracts/${contract.value.id}/terminate`); alert('合同已终止'); await fetchContract() }
  catch (err) { alert('终止失败：' + (err.response?.data?.message || err.message)) }
}

async function downloadFile() {
  try { const r = await axios.get(`/contracts/${contract.value.id}/download`, { responseType: 'blob' }); const u = window.URL.createObjectURL(new Blob([r.data])); const a = document.createElement('a'); a.href = u; a.setAttribute('download', contract.value.fileName); document.body.appendChild(a); a.click(); a.remove(); window.URL.revokeObjectURL(u) }
  catch (err) { alert('下载失败') }
}

function getStatusClass(c) { if (c.isFullyPaid) return 'status-complete'; if (c.paidAmount > 0) return 'status-partial'; return 'status-unpaid' }
function getStatusText(c) { if (c.isFullyPaid) return '✓ 已完成'; if (c.paidAmount > 0) return '进行中'; return '未支付' }
function getContractStatusLabel(s) { return { 0:'初始', 1:'进行中', 2:'已完成', 3:'已终止' }[s] ?? '未知' }
function getPaymentStatusText(s) { return { 0:'待审批', 1:'已通过', 2:'已驳回' }[s] ?? '未知' }
function getProgress() { if (!contract.value || contract.value.totalAmount === 0) return 0; return Math.round((contract.value.paidAmount / contract.value.totalAmount) * 100) }
function formatAmount(a) { return a.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function formatDate(d) { return new Date(d).toLocaleDateString('zh-CN') }
function formatDateTime(d) { return new Date(d).toLocaleString('zh-CN') }

function getLogActionLabel(a) {
  const m = { ContractCreated:'创建合同', ContractEdited:'编辑合同', ContractApproved:'审批通过', ContractRejected:'审批驳回', ContractTerminated:'终止合同', ContractDeleted:'删除合同', AmountSubmitted:'提交金额变更', AmountApproved:'金额审批通过', AmountRejected:'金额审批驳回', PaymentAdded:'添加支付', PaymentApproved:'支付审批通过', PaymentRejected:'支付审批驳回' }
  return m[a] ?? a
}
function getLogDotClass(a) {
  if (a.includes('Approved') || a === 'ContractCreated') return 'dot-green'
  if (a.includes('Rejected') || a === 'ContractTerminated') return 'dot-red'
  return 'dot-blue'
}

onMounted(() => { fetchContract() })
</script>

<style scoped>
.contract-detail-view { max-width: 1000px; margin: 0 auto; }
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 32px; }
.page-header h2 { margin: 0; font-size: 28px; color: #2c3e50; }
.header-actions { display: flex; gap: 12px; align-items: center; }
.back-btn { padding: 10px 20px; background: #ecf0f1; color: #2c3e50; text-decoration: none; border-radius: 6px; font-weight: 500; }
.back-btn:hover { background: #bdc3c7; }
.terminate-btn { padding: 10px 20px; background: #e74c3c; color: white; border: none; border-radius: 6px; cursor: pointer; font-weight: 600; }
.terminate-btn:hover { background: #c0392b; }
.terminated-banner { padding: 16px; background: #f8d7da; color: #721c24; border-radius: 8px; font-weight: 600; margin-bottom: 20px; text-align: center; border: 2px solid #f5c6cb; }
.loading, .error { text-align: center; padding: 60px 20px; font-size: 16px; }
.error { color: #e74c3c; }
.detail-container { display: flex; flex-direction: column; gap: 24px; }
.info-card, .amount-card, .payments-card, .logs-card { background: white; border-radius: 12px; padding: 24px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); }
.card-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
.card-header h3 { margin: 0; font-size: 20px; color: #2c3e50; }
.badges { display: flex; gap: 8px; }
.status-badge { padding: 8px 16px; border-radius: 20px; font-size: 14px; font-weight: 600; }
.status-complete { background: #d4edda; color: #27ae60; }
.status-partial { background: #fff3cd; color: #f39c12; }
.status-unpaid { background: #f8d7da; color: #e74c3c; }
.contract-status-badge { padding: 8px 16px; border-radius: 20px; font-size: 14px; font-weight: 600; }
.cs-0 { background: #e2e3e5; color: #383d41; }
.cs-1 { background: #cce5ff; color: #004085; }
.cs-2 { background: #d4edda; color: #155724; }
.cs-3 { background: #f5c6cb; color: #721c24; }
.info-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 20px; }
.info-item { display: flex; flex-direction: column; gap: 8px; }
.info-item.full-width { grid-column: 1 / -1; }
.info-item .label { font-size: 13px; color: #95a5a6; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; }
.info-item .value { font-size: 16px; color: #2c3e50; font-weight: 500; }
.file-download { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.filename { flex: 1; color: #667eea; font-weight: 500; }
.download-btn { padding: 8px 16px; background: #3498db; color: white; border: none; border-radius: 6px; cursor: pointer; font-weight: 500; }
.download-btn:hover { background: #2980b9; }
.amount-summary { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; margin-bottom: 24px; }
.amount-box { padding: 20px; border-radius: 10px; text-align: center; }
.amount-box.total { background: rgba(52,152,219,0.1); border: 2px solid #3498db; }
.amount-box.paid { background: rgba(39,174,96,0.1); border: 2px solid #27ae60; }
.amount-box.remaining { background: rgba(230,126,34,0.1); border: 2px solid #e67e22; }
.amount-label { display: block; font-size: 13px; color: #7f8c8d; margin-bottom: 8px; }
.amount-value { display: block; font-size: 24px; font-weight: 700; color: #2c3e50; }
.submit-amount-section { margin: 20px 0; padding: 16px; background: #f0f4ff; border-radius: 8px; border: 1px solid #d0d8ff; }
.submit-amount-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
.submit-amount-header h4 { margin: 0; color: #2c3e50; }
.toggle-btn { padding: 8px 16px; background: #667eea; color: white; border: none; border-radius: 6px; cursor: pointer; font-weight: 600; font-size: 13px; }
.submit-amount-form { display: flex; gap: 12px; align-items: center; }
.submit-amount-form .amount-input { flex: 1; }
.submit-amount-form .amount-input input { width: 100%; padding: 10px 14px 10px 32px; border: 2px solid #d0d8ff; border-radius: 6px; font-size: 15px; }
.pending-info { margin-top: 12px; padding: 8px 12px; background: #fff3cd; border-radius: 6px; color: #856404; font-size: 14px; }
.progress-section { margin-top: 16px; }
.progress-bar { height: 12px; background: #ecf0f1; border-radius: 6px; overflow: hidden; margin-bottom: 8px; }
.progress-fill { height: 100%; transition: width 0.5s ease; }
.progress-fill.status-complete { background: linear-gradient(90deg, #27ae60, #2ecc71); }
.progress-fill.status-partial { background: linear-gradient(90deg, #f39c12, #f1c40f); }
.progress-fill.status-unpaid { background: linear-gradient(90deg, #e74c3c, #c0392b); }
.progress-text { text-align: center; font-size: 14px; color: #7f8c8d; font-weight: 600; }
.add-payment-btn { padding: 10px 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border: none; border-radius: 6px; cursor: pointer; font-weight: 600; }
.add-payment-form { padding: 20px; background: #f8f9fa; border-radius: 8px; margin-bottom: 20px; }
.form-row { display: grid; grid-template-columns: repeat(2, 1fr); gap: 16px; }
.form-group { display: flex; flex-direction: column; gap: 8px; margin-bottom: 16px; }
.form-group label { font-weight: 600; color: #2c3e50; font-size: 14px; }
.form-group input, .form-group textarea { padding: 10px 14px; border: 2px solid #ecf0f1; border-radius: 6px; font-size: 14px; font-family: inherit; }
.form-group input:focus, .form-group textarea:focus { outline: none; border-color: #667eea; }
.amount-input { position: relative; }
.currency { position: absolute; left: 14px; top: 50%; transform: translateY(-50%); font-weight: 600; color: #7f8c8d; }
.amount-input input { padding-left: 32px; }
.hint { font-size: 12px; color: #95a5a6; }
.submit-btn { padding: 12px 24px; background: #27ae60; color: white; border: none; border-radius: 6px; cursor: pointer; font-weight: 600; font-size: 14px; }
.submit-btn:hover:not(:disabled) { background: #229954; }
.submit-btn:disabled { opacity: 0.6; cursor: not-allowed; }
.error-message { margin-top: 12px; padding: 10px 14px; background: #fee; border: 1px solid #fcc; border-radius: 6px; color: #c00; font-size: 13px; }
.empty-payments { text-align: center; padding: 40px 20px; color: #95a5a6; }
.payments-list { display: flex; flex-direction: column; gap: 12px; }
.payment-item { display: flex; align-items: center; gap: 16px; padding: 16px; background: #f8f9fa; border-radius: 8px; border-left: 4px solid #27ae60; }
.payment-item.ps-0 { border-left-color: #f39c12; }
.payment-item.ps-2 { border-left-color: #e74c3c; opacity: 0.7; }
.payment-icon { font-size: 32px; }
.payment-info { flex: 1; }
.payment-amount { font-size: 18px; font-weight: 700; color: #27ae60; margin-bottom: 4px; }
.payment-date { font-size: 14px; color: #7f8c8d; }
.payment-note { font-size: 13px; color: #95a5a6; font-style: italic; }
.payment-status-col { min-width: 70px; text-align: center; }
.pay-status-badge { display: inline-block; padding: 4px 10px; border-radius: 12px; font-size: 12px; font-weight: 600; }
.pst-0 { background: #fff3cd; color: #856404; }
.pst-1 { background: #d4edda; color: #155724; }
.pst-2 { background: #f8d7da; color: #721c24; }
.payment-time { font-size: 12px; color: #95a5a6; white-space: nowrap; }
/* 操作日志时间线 */
.timeline { display: flex; flex-direction: column; gap: 0; position: relative; padding-left: 24px; }
.timeline-item { position: relative; padding: 12px 0 12px 20px; border-left: 2px solid #ecf0f1; }
.timeline-item:last-child { border-left-color: transparent; }
.timeline-dot { position: absolute; left: -7px; top: 18px; width: 12px; height: 12px; border-radius: 50%; border: 2px solid white; box-shadow: 0 0 0 2px #ecf0f1; }
.dot-green { background: #27ae60; }
.dot-red { background: #e74c3c; }
.dot-blue { background: #3498db; }
.timeline-content { background: #f8f9fa; padding: 12px 16px; border-radius: 8px; }
.timeline-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }
.log-action { font-weight: 600; color: #2c3e50; font-size: 14px; }
.log-time { font-size: 12px; color: #95a5a6; }
.log-desc { font-size: 13px; color: #5a6c7d; margin-bottom: 4px; }
.log-user { font-size: 12px; color: #95a5a6; }
.slide-fade-enter-active, .slide-fade-leave-active { transition: all 0.3s ease; }
.slide-fade-enter-from { opacity: 0; transform: translateY(-10px); }
.slide-fade-leave-to { opacity: 0; transform: translateY(-10px); }
@media (max-width: 768px) {
  .info-grid, .amount-summary, .form-row { grid-template-columns: 1fr; }
  .page-header { flex-direction: column; align-items: stretch; gap: 16px; }
  .header-actions { justify-content: flex-end; }
}
</style>
