<template>
  <div class="contract-edit-view">
    <div class="page-header">
      <h2>编辑合同</h2>
      <router-link to="/contracts/list" class="back-btn">
        ← 返回列表
      </router-link>
    </div>

    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="error" class="error">{{ error }}</div>

    <div v-else-if="contract" class="form-container">
      <form @submit.prevent="handleSubmit" class="contract-form">
        <div class="form-group">
          <label for="name">合同名称 <span class="required">*</span></label>
          <input
            id="name"
            v-model="form.name"
            type="text"
            placeholder="请输入合同名称"
            required
          />
        </div>

        <div class="form-group">
          <label for="description">合同描述</label>
          <textarea
            id="description"
            v-model="form.description"
            rows="4"
            placeholder="请输入合同描述（选填）"
          ></textarea>
        </div>

        <!-- 原始金额显示 -->
        <div class="info-box">
          <div class="info-label">📋 原始金额（参考）</div>
          <div class="info-value">¥{{ formatAmount(contract.originalAmount) }}</div>
        </div>

        <!-- 合同总金额编辑 -->
        <div class="form-group">
          <label for="totalAmount">合同总金额 <span class="required">*</span></label>
          <div 
            class="amount-input" 
            :class="getAmountInputClass()"
          >
            <span class="currency">¥</span>
            <input
              id="totalAmount"
              v-model.number="form.totalAmount"
              type="number"
              step="0.01"
              min="0"
              placeholder="0.00"
              required
              @input="calculateDifference"
            />
            <span class="amount-status-icon">{{ getAmountStatusIcon() }}</span>
          </div>
          
          <!-- 差额显示 -->
          <div v-if="amountDifference !== 0" class="amount-hint" :class="getDifferenceClass()">
            {{ getDifferenceText() }}
          </div>
        </div>

        <div class="form-actions">
          <button type="submit" class="submit-btn" :disabled="submitting">
            <span v-if="submitting">保存中...</span>
            <span v-else>💾 保存修改</span>
          </button>
          <router-link to="/contracts/list" class="cancel-btn">
            ✕ 取消
          </router-link>
        </div>

        <div v-if="submitError" class="error-message">{{ submitError }}</div>
        <div v-if="success" class="success-message">{{ success }}</div>
      </form>
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
const loading = ref(false)
const error = ref(null)
const submitting = ref(false)
const submitError = ref(null)
const success = ref(null)

const form = ref({
  name: '',
  description: '',
  totalAmount: 0
})

const amountDifference = ref(0)

async function fetchContract() {
  loading.value = true
  error.value = null
  try {
    const id = route.params.id
    const response = await axios.get(`/api/contracts/${id}`)
    contract.value = response.data
    
    // 预填充表单
    form.value = {
      name: response.data.name,
      description: response.data.description || '',
      totalAmount: response.data.totalAmount
    }
    
    calculateDifference()
  } catch (err) {
    error.value = '加载合同失败：' + (err.response?.data?.message || err.message)
  } finally {
    loading.value = false
  }
}

function calculateDifference() {
  if (contract.value) {
    amountDifference.value = form.value.totalAmount - contract.value.originalAmount
  }
}

function getAmountInputClass() {
  if (amountDifference.value > 0) {
    return 'amount-increased' // 绿色
  } else if (amountDifference.value < 0) {
    return 'amount-decreased' // 红色
  }
  return '' // 无变化
}

function getDifferenceClass() {
  return amountDifference.value >= 0 ? 'positive-diff' : 'negative-diff'
}

function getAmountStatusIcon() {
  if (amountDifference.value > 0) return '✓'
  if (amountDifference.value < 0) return '⚠️'
  return '='
}

function getDifferenceText() {
  const absAmount = Math.abs(amountDifference.value)
  const formatted = formatAmount(absAmount)
  
  if (amountDifference.value > 0) {
    return `增加 ¥${formatted}`
  } else {
    return `减少 ¥${formatted}`
  }
}

async function handleSubmit() {
  submitError.value = null
  success.value = null
  submitting.value = true

  try {
    await axios.put(`/api/contracts/${contract.value.id}`, {
      name: form.value.name,
      description: form.value.description,
      totalAmount: form.value.totalAmount
    })

    success.value = '合同更新成功！即将跳转...'
    
    setTimeout(() => {
      router.push('/contracts/list')
    }, 1500)
  } catch (err) {
    submitError.value = '更新失败：' + (err.response?.data?.message || err.message)
  } finally {
    submitting.value = false
  }
}

function formatAmount(amount) {
  return amount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

onMounted(() => {
  fetchContract()
})
</script>

<style scoped>
.contract-edit-view {
  max-width: 800px;
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

.form-container {
  background: white;
  border-radius: 12px;
  padding: 32px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.contract-form {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.form-group label {
  font-weight: 600;
  color: #2c3e50;
  font-size: 14px;
}

.required {
  color: #e74c3c;
}

.form-group input[type="text"],
.form-group textarea {
  padding: 12px 16px;
  border: 2px solid #ecf0f1;
  border-radius: 8px;
  font-size: 14px;
  transition: border-color 0.3s;
  font-family: inherit;
}

.form-group input[type="text"]:focus,
.form-group textarea:focus {
  outline: none;
  border-color: #667eea;
}

.info-box {
  padding: 16px 20px;
  background: linear-gradient(135deg, rgba(52, 152, 219, 0.1) 0%, rgba(41, 128, 185, 0.05) 100%);
  border-left: 4px solid #3498db;
  border-radius: 8px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.info-label {
  font-size: 14px;
  color: #2c3e50;
  font-weight: 600;
}

.info-value {
  font-size: 20px;
  font-weight: 700;
  color: #3498db;
}

.amount-input {
  position: relative;
  display: flex;
  align-items: center;
  transition: all 0.3s;
}

.currency {
  position: absolute;
  left: 16px;
  font-weight: 600;
  color: #7f8c8d;
  font-size: 16px;
}

.amount-status-icon {
  position: absolute;
  right: 16px;
  font-size: 20px;
}

.amount-input input {
  padding: 12px 50px 12px 40px;
  border: 3px solid #ecf0f1;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 600;
  width: 100%;
  transition: all 0.3s;
}

.amount-input input:focus {
  outline: none;
}

/* 金额增加 - 绿色 */
.amount-input.amount-increased input {
  border-color: #27ae60;
  background: linear-gradient(135deg, rgba(39, 174, 96, 0.05) 0%, rgba(46, 204, 113, 0.02) 100%);
}

.amount-input.amount-increased input:focus {
  border-color: #27ae60;
  box-shadow: 0 0 0 3px rgba(39, 174, 96, 0.1);
}

.amount-input.amount-increased .amount-status-icon {
  color: #27ae60;
}

/* 金额减少 - 红色 */
.amount-input.amount-decreased input {
  border-color: #e74c3c;
  background: linear-gradient(135deg, rgba(231, 76, 60, 0.05) 0%, rgba(192, 57, 43, 0.02) 100%);
}

.amount-input.amount-decreased input:focus {
  border-color: #e74c3c;
  box-shadow: 0 0 0 3px rgba(231, 76, 60, 0.1);
}

.amount-input.amount-decreased .amount-status-icon {
  color: #e74c3c;
}

.amount-hint {
  font-size: 14px;
  font-weight: 600;
  padding: 8px 12px;
  border-radius: 6px;
  margin-top: 4px;
}

.amount-hint.positive-diff {
  color: #27ae60;
  background: rgba(39, 174, 96, 0.1);
}

.amount-hint.negative-diff {
  color: #e74c3c;
  background: rgba(231, 76, 60, 0.1);
}

.form-actions {
  display: flex;
  gap: 12px;
  margin-top: 16px;
}

.submit-btn,
.cancel-btn {
  flex: 1;
  padding: 14px 24px;
  border: none;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
  text-align: center;
  text-decoration: none;
  display: inline-block;
}

.submit-btn {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
}

.submit-btn:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 6px 16px rgba(102, 126, 234, 0.4);
}

.submit-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.cancel-btn {
  background: #ecf0f1;
  color: #2c3e50;
}

.cancel-btn:hover {
  background: #bdc3c7;
}

.error-message {
  padding: 12px 16px;
  background: #fee;
  border: 1px solid #fcc;
  border-radius: 6px;
  color: #c00;
  font-size: 14px;
}

.success-message {
  padding: 12px 16px;
  background: #d4edda;
  border: 1px solid #c3e6cb;
  border-radius: 6px;
  color: #155724;
  font-size: 14px;
}

@media (max-width: 768px) {
  .form-container {
    padding: 20px;
  }

  .page-header {
    flex-direction: column;
    align-items: stretch;
    gap: 16px;
  }

  .back-btn {
    text-align: center;
  }

  .form-actions {
    flex-direction: column;
  }

  .info-box {
    flex-direction: column;
    gap: 8px;
    text-align: center;
  }
}
</style>
