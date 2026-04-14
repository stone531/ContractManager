<template>
  <div class="contract-list-view">
    <div class="page-header">
      <h2>合同查询</h2>
      <router-link to="/contracts/add" class="add-btn">
        <span>➕</span> 新增合同
      </router-link>
    </div>

    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="contracts.length === 0" class="empty">暂无合同数据</div>

    <div v-else class="contracts-grid">
      <div 
        v-for="contract in contracts" 
        :key="contract.id" 
        class="contract-card"
        :class="{ 'fully-paid': contract.isFullyPaid }"
        @click="viewContract(contract.id)"
      >
        <div class="card-header">
          <div class="contract-name">
            <span class="contract-icon">📄</span>
            <h3>{{ contract.name }}</h3>
          </div>
          <div class="payment-status" :class="getStatusClass(contract)">
            {{ getStatusText(contract) }}
          </div>
        </div>

        <div class="card-body">
          <p v-if="contract.description" class="description">{{ contract.description }}</p>
          
          <div class="amount-info">
            <div class="amount-item">
              <span class="label">合同总额</span>
              <span class="value total">¥{{ formatAmount(contract.totalAmount) }}</span>
            </div>
            <div class="amount-item">
              <span class="label">已支付</span>
              <span class="value paid">¥{{ formatAmount(contract.paidAmount) }}</span>
            </div>
            <div class="amount-item">
              <span class="label">剩余</span>
              <span class="value remaining">¥{{ formatAmount(contract.remainingAmount) }}</span>
            </div>
          </div>

          <div class="progress-bar">
            <div 
              class="progress-fill" 
              :style="{ width: getProgress(contract) + '%' }"
              :class="getStatusClass(contract)"
            ></div>
          </div>
          <div class="progress-text">{{ getProgress(contract) }}%</div>

          <div class="card-footer">
            <span v-if="contract.fileName" class="file-info">
              📎 {{ contract.fileName }}
            </span>
            <span class="date">{{ formatDate(contract.createdAt) }}</span>
          </div>
        </div>

        <div class="card-actions" @click.stop>
          <button 
            @click="editContract(contract.id)"
            class="action-btn edit-btn"
            title="编辑合同"
          >
            ✏️ 编辑
          </button>
          <button 
            v-if="contract.fileName" 
            @click="downloadFile(contract.id, contract.fileName)"
            class="action-btn download-btn"
            title="下载合同"
          >
            📥 下载
          </button>
          <button 
            @click="viewContract(contract.id)"
            class="action-btn detail-btn"
            title="查看详情"
          >
            👁️ 详情
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import axios from '../api/axios'

const router = useRouter()
const contracts = ref([])
const loading = ref(false)
const error = ref(null)

async function fetchContracts() {
  loading.value = true
  error.value = null
  try {
    const response = await axios.get('/api/contracts')
    contracts.value = response.data
  } catch (err) {
    error.value = '加载合同失败：' + (err.response?.data?.message || err.message)
  } finally {
    loading.value = false
  }
}

function viewContract(id) {
  router.push(`/contracts/${id}`)
}

function editContract(id) {
  router.push(`/contracts/edit/${id}`)
}

async function downloadFile(id, filename) {
  try {
    const response = await axios.get(`/api/contracts/${id}/download`, {
      responseType: 'blob'
    })
    
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', filename)
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
  } catch (err) {
    alert('下载失败：' + (err.response?.data?.message || err.message))
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

function getProgress(contract) {
  if (contract.totalAmount === 0) return 0
  return Math.round((contract.paidAmount / contract.totalAmount) * 100)
}

function formatAmount(amount) {
  return amount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatDate(dateString) {
  return new Date(dateString).toLocaleDateString('zh-CN')
}

onMounted(() => {
  fetchContracts()
})
</script>

<style scoped>
.contract-list-view {
  max-width: 1400px;
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

.add-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 12px 24px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  text-decoration: none;
  border-radius: 8px;
  font-weight: 600;
  transition: all 0.3s;
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
}

.add-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 16px rgba(102, 126, 234, 0.4);
}

.loading,
.error,
.empty {
  text-align: center;
  padding: 60px 20px;
  font-size: 16px;
  color: #7f8c8d;
}

.error {
  color: #e74c3c;
}

.contracts-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(380px, 1fr));
  gap: 24px;
}

.contract-card {
  background: white;
  border-radius: 12px;
  padding: 24px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  transition: all 0.3s;
  cursor: pointer;
  border: 2px solid transparent;
}

.contract-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
  border-color: #667eea;
}

.contract-card.fully-paid {
  border-color: #27ae60;
  background: linear-gradient(to bottom, white 0%, rgba(39, 174, 96, 0.02) 100%);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 16px;
  gap: 16px;
}

.contract-name {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
}

.contract-icon {
  font-size: 28px;
}

.contract-name h3 {
  margin: 0;
  font-size: 18px;
  color: #2c3e50;
  word-break: break-word;
}

.payment-status {
  padding: 6px 12px;
  border-radius: 20px;
  font-size: 13px;
  font-weight: 600;
  white-space: nowrap;
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

.card-body {
  margin-bottom: 16px;
}

.description {
  color: #7f8c8d;
  font-size: 14px;
  margin: 0 0 16px 0;
  line-height: 1.5;
}

.amount-info {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
  margin-bottom: 16px;
}

.amount-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.amount-item .label {
  font-size: 12px;
  color: #95a5a6;
}

.amount-item .value {
  font-size: 16px;
  font-weight: 600;
}

.value.total {
  color: #3498db;
}

.value.paid {
  color: #27ae60;
}

.value.remaining {
  color: #e67e22;
}

.progress-bar {
  height: 8px;
  background: #ecf0f1;
  border-radius: 4px;
  overflow: hidden;
  margin-bottom: 8px;
}

.progress-fill {
  height: 100%;
  transition: width 0.3s ease;
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
  font-size: 12px;
  color: #95a5a6;
  margin-bottom: 12px;
}

.card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-top: 12px;
  border-top: 1px solid #ecf0f1;
  font-size: 13px;
  color: #95a5a6;
  margin-bottom: 12px;
}

.file-info {
  display: flex;
  align-items: center;
  gap: 4px;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.card-actions {
  display: flex;
  gap: 8px;
}

.action-btn {
  flex: 1;
  padding: 8px 16px;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.3s;
  font-weight: 500;
}

.download-btn {
  background: #3498db;
  color: white;
}

.download-btn:hover {
  background: #2980b9;
  transform: translateY(-1px);
}

.detail-btn {
  background: #667eea;
  color: white;
}

.edit-btn {
  background: #f39c12;
  color: white;
}

.edit-btn:hover {
  background: #e67e22;
  transform: translateY(-1px);
}

.detail-btn:hover {
  background: #5568d3;
  transform: translateY(-1px);
}

@media (max-width: 768px) {
  .contracts-grid {
    grid-template-columns: 1fr;
  }

  .page-header {
    flex-direction: column;
    align-items: stretch;
    gap: 16px;
  }

  .add-btn {
    justify-content: center;
  }
}
</style>
