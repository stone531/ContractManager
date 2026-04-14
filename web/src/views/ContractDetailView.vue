<template>
  <div class="contract-detail-view">
    <div class="page-header">
      <h2>合同详情</h2>
      <router-link to="/contracts/list" class="back-btn">
        ← 返回列表
      </router-link>
    </div>

    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    
    <div v-else-if="contract" class="detail-container">
      <!-- 合同信息卡片 -->
      <div class="info-card">
        <div class="card-header">
          <h3>📄 合同信息</h3>
          <div class="status-badge" :class="getStatusClass(contract)">
            {{ getStatusText(contract) }}
          </div>
        </div>

        <div class="info-grid">
          <div class="info-item">
            <span class="label">合同名称</span>
            <span class="value">{{ contract.name }}</span>
          </div>
          
          <div class="info-item">
            <span class="label">创建时间</span>
            <span class="value">{{ formatDate(contract.createdAt) }}</span>
          </div>

          <div v-if="contract.description" class="info-item full-width">
            <span class="label">合同描述</span>
            <span class="value">{{ contract.description }}</span>
          </div>

          <div v-if="contract.fileName" class="info-item full-width">
            <span class="label">合同文件</span>
            <div class="file-download">
              <span class="filename">📎 {{ contract.fileName }}</span>
              <button @click="downloadFile" class="download-btn">
                📥 下载
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- 金额统计卡片 -->
      <div class="amount-card">
        <h3>💰 金额统计</h3>
        
        <div class="amount-summary">
          <div class="amount-box total">
            <span class="amount-label">合同总额</span>
            <span class="amount-value">¥{{ formatAmount(contract.totalAmount) }}</span>
          </div>
          <div class="amount-box paid">
            <span class="amount-label">已支付</span>
            <span class="amount-value">¥{{ formatAmount(contract.paidAmount) }}</span>
          </div>
          <div class="amount-box remaining">
            <span class="amount-label">剩余应付</span>
            <span class="amount-value">¥{{ formatAmount(contract.remainingAmount) }}</span>
          </div>
        </div>

        <div class="progress-section">
          <div class="progress-bar">
            <div 
              class="progress-fill" 
              :style="{ width: getProgress() + '%' }"
              :class="getStatusClass(contract)"
            ></div>
          </div>
          <div class="progress-text">完成度: {{ getProgress() }}%</div>
        </div>
      </div>

      <!-- 支付记录 -->
      <div class="payments-card">
        <div class="card-header">
          <h3>💳 支付记录</h3>
          <button @click="showAddPayment = !showAddPayment" class="add-payment-btn">
            {{ showAddPayment ? '✕ 取消' : '➕ 添加支付' }}
          </button>
        </div>

        <!-- 添加支付表单 -->
        <transition name="slide-fade">
          <div v-if="showAddPayment" class="add-payment-form">
            <form @submit.prevent="handleAddPayment">
              <div class="form-row">
                <div class="form-group">
                  <label>支付金额 *</label>
                  <div class="amount-input">
                    <span class="currency">¥</span>
                    <input
                      v-model.number="paymentForm.amount"
                      type="number"
                      step="0.01"
                      min="0.01"
                      :max="contract.remainingAmount"
                      required
                      placeholder="0.00"
                    />
                  </div>
                  <span class="hint">剩余可支付: ¥{{ formatAmount(contract.remainingAmount) }}</span>
                </div>

                <div class="form-group">
                  <label>支付日期 *</label>
                  <input
                    v-model="paymentForm.paymentDate"
                    type="date"
                    required
                  />
                </div>
              </div>

              <div class="form-group">
                <label>备注说明</label>
                <textarea
                  v-model="paymentForm.note"
                  rows="2"
                  placeholder="请输入备注（选填）"
                ></textarea>
              </div>

              <div class="form-actions">
                <button type="submit" class="submit-btn" :disabled="submitting">
                  {{ submitting ? '提交中...' : '✓ 确认添加' }}
                </button>
              </div>

              <div v-if="paymentError" class="error-message">{{ paymentError }}</div>
            </form>
          </div>
        </transition>

        <!-- 支付记录列表 -->
        <div v-if="payments.length === 0" class="empty-payments">
          暂无支付记录
        </div>
        <div v-else class="payments-list">
          <div v-for="payment in payments" :key="payment.id" class="payment-item">
            <div class="payment-icon">💵</div>
            <div class="payment-info">
              <div class="payment-amount">¥{{ formatAmount(payment.amount) }}</div>
              <div class="payment-date">{{ formatDate(payment.paymentDate) }}</div>
              <div v-if="payment.note" class="payment-note">{{ payment.note }}</div>
            </div>
            <div class="payment-time">
              {{ formatDateTime(payment.createdAt) }}
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

const route = useRoute()
const router = useRouter()

const contract = ref(null)
const payments = ref([])
const loading = ref(false)
const error = ref(null)
const showAddPayment = ref(false)
const submitting = ref(false)
const paymentError = ref(null)

const paymentForm = ref({
  amount: 0,
  paymentDate: new Date().toISOString().split('T')[0],
  note: ''
})

async function fetchContract() {
  loading.value = true
  error.value = null
  try {
    const id = route.params.id
    const response = await axios.get(`/api/contracts/${id}`)
    contract.value = response.data
    payments.value = response.data.payments || []
  } catch (err) {
    error.value = '加载合同失败：' + (err.response?.data?.message || err.message)
  } finally {
    loading.value = false
  }
}

async function downloadFile() {
  try {
    const response = await axios.get(`/api/contracts/${contract.value.id}/download`, {
      responseType: 'blob'
    })
    
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', contract.value.fileName)
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
  } catch (err) {
    alert('下载失败：' + (err.response?.data?.message || err.message))
  }
}

async function handleAddPayment() {
  paymentError.value = null
  submitting.value = true

  try {
    await axios.post(`/api/contracts/${contract.value.id}/payments`, paymentForm.value)
    
    // 重新加载数据
    await fetchContract()
    
    // 重置表单
    paymentForm.value = {
      amount: 0,
      paymentDate: new Date().toISOString().split('T')[0],
      note: ''
    }
    
    showAddPayment.value = false
    alert('支付记录添加成功！')
  } catch (err) {
    paymentError.value = '添加失败：' + (err.response?.data || err.message)
  } finally {
    submitting.value = false
  }
}

function getStatusClass(contract) {
  if (contract.isFullyPaid) return 'status-complete'
  if (contract.paidAmount > 0) return 'status-partial'
  return 'status-unpaid'
}

function getStatusText(contract) {
  if (contract.isFullyPaid) return '✓ 已完成'
  if (contract.paidAmount > 0) return '进行中'
  return '未支付'
}

function getProgress() {
  if (!contract.value || contract.value.totalAmount === 0) return 0
  return Math.round((contract.value.paidAmount / contract.value.totalAmount) * 100)
}

function formatAmount(amount) {
  return amount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatDate(dateString) {
  return new Date(dateString).toLocaleDateString('zh-CN')
}

function formatDateTime(dateString) {
  return new Date(dateString).toLocaleString('zh-CN')
}

onMounted(() => {
  fetchContract()
})
</script>

<style scoped>
.contract-detail-view {
  max-width: 1000px;
  margin: 0 auto;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 32px;
}

.page-header h2 {
  margin: 0;
  font-size: 28px;
  color: #2c3e50;
}

.back-btn {
  padding: 10px 20px;
  background: #ecf0f1;
  color: #2c3e50;
  text-decoration: none;
  border-radius: 6px;
  transition: all 0.3s;
  font-weight: 500;
}

.back-btn:hover {
  background: #bdc3c7;
}

.loading,
.error {
  text-align: center;
  padding: 60px 20px;
  font-size: 16px;
}

.error {
  color: #e74c3c;
}

.detail-container {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.info-card,
.amount-card,
.payments-card {
  background: white;
  border-radius: 12px;
  padding: 24px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.card-header h3 {
  margin: 0;
  font-size: 20px;
  color: #2c3e50;
}

.status-badge {
  padding: 8px 16px;
  border-radius: 20px;
  font-size: 14px;
  font-weight: 600;
}

.status-complete {
  background: #d4edda;
  color: #27ae60;
}

.status-partial {
  background: #fff3cd;
  color: #f39c12;
}

.status-unpaid {
  background: #f8d7da;
  color: #e74c3c;
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 20px;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.info-item.full-width {
  grid-column: 1 / -1;
}

.info-item .label {
  font-size: 13px;
  color: #95a5a6;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.info-item .value {
  font-size: 16px;
  color: #2c3e50;
  font-weight: 500;
}

.file-download {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.filename {
  flex: 1;
  color: #667eea;
  font-weight: 500;
}

.download-btn {
  padding: 8px 16px;
  background: #3498db;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.3s;
  font-weight: 500;
}

.download-btn:hover {
  background: #2980b9;
  transform: translateY(-1px);
}

.amount-summary {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
  margin-bottom: 24px;
}

.amount-box {
  padding: 20px;
  border-radius: 10px;
  text-align: center;
}

.amount-box.total {
  background: linear-gradient(135deg, rgba(52, 152, 219, 0.1) 0%, rgba(41, 128, 185, 0.05) 100%);
  border: 2px solid #3498db;
}

.amount-box.paid {
  background: linear-gradient(135deg, rgba(39, 174, 96, 0.1) 0%, rgba(46, 204, 113, 0.05) 100%);
  border: 2px solid #27ae60;
}

.amount-box.remaining {
  background: linear-gradient(135deg, rgba(230, 126, 34, 0.1) 0%, rgba(241, 196, 15, 0.05) 100%);
  border: 2px solid #e67e22;
}

.amount-label {
  display: block;
  font-size: 13px;
  color: #7f8c8d;
  margin-bottom: 8px;
}

.amount-value {
  display: block;
  font-size: 24px;
  font-weight: 700;
  color: #2c3e50;
}

.progress-section {
  margin-top: 16px;
}

.progress-bar {
  height: 12px;
  background: #ecf0f1;
  border-radius: 6px;
  overflow: hidden;
  margin-bottom: 8px;
}

.progress-fill {
  height: 100%;
  transition: width 0.5s ease;
}

.progress-fill.status-complete {
  background: linear-gradient(90deg, #27ae60, #2ecc71);
}

.progress-fill.status-partial {
  background: linear-gradient(90deg, #f39c12, #f1c40f);
}

.progress-fill.status-unpaid {
  background: linear-gradient(90deg, #e74c3c, #c0392b);
}

.progress-text {
  text-align: center;
  font-size: 14px;
  color: #7f8c8d;
  font-weight: 600;
}

.add-payment-btn {
  padding: 10px 20px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.3s;
  font-weight: 600;
}

.add-payment-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
}

.add-payment-form {
  padding: 20px;
  background: #f8f9fa;
  border-radius: 8px;
  margin-bottom: 20px;
}

.form-row {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 16px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 16px;
}

.form-group label {
  font-weight: 600;
  color: #2c3e50;
  font-size: 14px;
}

.form-group input,
.form-group textarea {
  padding: 10px 14px;
  border: 2px solid #ecf0f1;
  border-radius: 6px;
  font-size: 14px;
  transition: border-color 0.3s;
  font-family: inherit;
}

.form-group input:focus,
.form-group textarea:focus {
  outline: none;
  border-color: #667eea;
}

.amount-input {
  position: relative;
}

.currency {
  position: absolute;
  left: 14px;
  top: 50%;
  transform: translateY(-50%);
  font-weight: 600;
  color: #7f8c8d;
}

.amount-input input {
  padding-left: 32px;
}

.hint {
  font-size: 12px;
  color: #95a5a6;
}

.submit-btn {
  padding: 12px 24px;
  background: #27ae60;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.3s;
  font-weight: 600;
  font-size: 14px;
}

.submit-btn:hover:not(:disabled) {
  background: #229954;
  transform: translateY(-1px);
}

.submit-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error-message {
  margin-top: 12px;
  padding: 10px 14px;
  background: #fee;
  border: 1px solid #fcc;
  border-radius: 6px;
  color: #c00;
  font-size: 13px;
}

.empty-payments {
  text-align: center;
  padding: 40px 20px;
  color: #95a5a6;
}

.payments-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.payment-item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px;
  background: #f8f9fa;
  border-radius: 8px;
  border-left: 4px solid #27ae60;
  transition: all 0.3s;
}

.payment-item:hover {
  background: #e8f5e9;
  transform: translateX(4px);
}

.payment-icon {
  font-size: 32px;
}

.payment-info {
  flex: 1;
}

.payment-amount {
  font-size: 18px;
  font-weight: 700;
  color: #27ae60;
  margin-bottom: 4px;
}

.payment-date {
  font-size: 14px;
  color: #7f8c8d;
  margin-bottom: 4px;
}

.payment-note {
  font-size: 13px;
  color: #95a5a6;
  font-style: italic;
}

.payment-time {
  font-size: 12px;
  color: #95a5a6;
  white-space: nowrap;
}

.slide-fade-enter-active,
.slide-fade-leave-active {
  transition: all 0.3s ease;
}

.slide-fade-enter-from {
  opacity: 0;
  transform: translateY(-10px);
}

.slide-fade-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

@media (max-width: 768px) {
  .info-grid,
  .amount-summary,
  .form-row {
    grid-template-columns: 1fr;
  }

  .page-header {
    flex-direction: column;
    align-items: stretch;
    gap: 16px;
  }

  .back-btn {
    text-align: center;
  }
}
</style>
