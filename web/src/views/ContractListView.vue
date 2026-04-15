<template>
  <div class="contract-list-view">
    <div class="page-header">
      <h2>合同查询</h2>
      <router-link to="/contracts/add" class="add-btn">
        <span>➕</span> 新增合同
      </router-link>
    </div>

    <!-- 查询条件栏 -->
    <div class="search-panel">
      <div class="search-group">
        <div class="form-item">
          <label>合同名称</label>
          <input
            v-model="searchForm.name"
            type="text"
            placeholder="输入合同名称"
            @keyup.enter="handleSearch"
          />
        </div>

        <div class="form-item">
          <label>合同编号</label>
          <input
            v-model="searchForm.number"
            type="text"
            placeholder="输入合同编号"
            @keyup.enter="handleSearch"
          />
        </div>

        <div class="form-item">
          <label>创建日期</label>
          <div class="date-range">
            <input
              v-model="searchForm.startDate"
              type="date"
              placeholder="起始日期"
            />
            <span class="separator">-</span>
            <input
              v-model="searchForm.endDate"
              type="date"
              placeholder="结束日期"
            />
          </div>
        </div>

        <div class="form-item">
          <label>审批状态</label>
          <select v-model="searchForm.approvalStatus">
            <option value="">全部</option>
            <option value="0">待审核</option>
            <option value="1">已通过</option>
            <option value="2">已拒绝</option>
          </select>
        </div>

        <div class="form-item">
          <label>合同状态</label>
          <select v-model="searchForm.contractStatus">
            <option value="">全部</option>
            <option value="0">初始</option>
            <option value="1">进行中</option>
            <option value="2">已完成</option>
            <option value="3">已终止</option>
          </select>
        </div>

        <div class="form-actions">
          <button @click="handleSearch" class="search-btn">🔍 搜索</button>
          <button @click="handleReset" class="reset-btn">🔄 重置</button>
        </div>
      </div>
    </div>

    <!-- 加载/错误/空状态 -->
    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="contracts.length === 0" class="empty">暂无合同数据</div>

    <!-- 表格展示 -->
    <div v-else class="table-container">
      <table class="contracts-table">
        <thead>
          <tr>
            <th>ID</th>
            <th>合同编号</th>
            <th>合同名称</th>
            <th>合同金额</th>
            <th>已支付</th>
            <th>进度</th>
            <th>审批状态</th>
            <th>合同状态</th>
            <th>创建时间</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="contract in contracts" :key="contract.id" :class="getRowClass(contract)">
            <td class="cell-id">{{ contract.id }}</td>
            <td class="cell-number">{{ contract.contractNumber || '-' }}</td>
            <td class="cell-name">{{ contract.name }}</td>
            <td class="cell-amount">¥{{ formatAmount(contract.totalAmount) }}</td>
            <td class="cell-paid">¥{{ formatAmount(contract.paidAmount) }}</td>
            <td class="cell-progress">
              <div class="progress-container">
                <div class="progress-bar">
                  <div
                    class="progress-fill"
                    :style="{ width: getProgress(contract) + '%' }"
                    :class="getStatusClass(contract)"
                  ></div>
                </div>
                <span class="progress-text">{{ getProgress(contract) }}%</span>
              </div>
            </td>
            <td class="cell-status">
              <span class="approval-badge" :class="'approval-' + contract.approvalStatus">
                {{ getApprovalText(contract.approvalStatus) }}
              </span>
            </td>
            <td class="cell-status">
              <span class="contract-status-badge" :class="'cs-' + contract.contractStatus">
                {{ getContractStatusText(contract.contractStatus) }}
              </span>
            </td>
            <td class="cell-date">{{ formatDate(contract.createdAt) }}</td>
            <td class="cell-actions">
              <div class="action-buttons">
                <button
                  @click="viewContract(contract.id)"
                  class="action-btn detail-btn"
                  title="查看详情"
                >
                  👁️
                </button>
                <button
                  @click="editContract(contract.id)"
                  class="action-btn edit-btn"
                  title="编辑合同"
                >
                  ✏️
                </button>
                <button
                  v-if="contract.fileName"
                  @click="previewFile(contract.id, contract.fileName)"
                  class="action-btn preview-btn"
                  title="预览合同"
                >
                  👁️📄
                </button>
                <button
                  v-if="contract.fileName"
                  @click="downloadFile(contract.id, contract.fileName)"
                  class="action-btn download-btn"
                  title="下载合同"
                >
                  📥
                </button>
                <button
                  v-if="isSuperAdmin"
                  @click="deleteContract(contract.id, contract.name)"
                  class="action-btn delete-btn"
                  title="删除合同"
                >
                  🗑️
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- 分页 -->
      <div class="pagination">
        <span class="pagination-info">共 {{ totalCount }} 条记录，第 {{ currentPage }} / {{ totalPages }} 页</span>
        <div class="pagination-buttons">
          <button
            class="page-btn"
            :disabled="currentPage <= 1"
            @click="goToPage(currentPage - 1)"
          >
            ◀ 上一页
          </button>
          <button
            class="page-btn"
            :disabled="currentPage >= totalPages"
            @click="goToPage(currentPage + 1)"
          >
            下一页 ▶
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import axios from '../api/axios'

const router = useRouter()
const authStore = useAuthStore()

const isSuperAdmin = computed(() => {
  const role = authStore.user?.role
  return role === 0 || role === 'SuperAdmin'
})
const contracts = ref([])
const loading = ref(false)
const error = ref(null)

// 分页状态
const currentPage = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

const totalPages = computed(() => {
  return Math.max(1, Math.ceil(totalCount.value / pageSize.value))
})

const searchForm = ref({
  name: '',
  number: '',
  startDate: '',
  endDate: '',
  approvalStatus: '',
  contractStatus: ''
})

async function fetchContracts() {
  loading.value = true
  error.value = null
  try {
    const response = await axios.get('/contracts', {
      params: {
        name: searchForm.value.name || undefined,
        number: searchForm.value.number || undefined,
        startDate: searchForm.value.startDate || undefined,
        endDate: searchForm.value.endDate || undefined,
        approvalStatus: searchForm.value.approvalStatus !== '' ? searchForm.value.approvalStatus : undefined,
        contractStatus: searchForm.value.contractStatus !== '' ? searchForm.value.contractStatus : undefined,
        page: currentPage.value,
        pageSize: pageSize.value
      }
    })
    contracts.value = response.data.items
    totalCount.value = response.data.total
  } catch (err) {
    error.value = '加载合同失败：' + (err.response?.data?.message || err.message)
  } finally {
    loading.value = false
  }
}

function handleSearch() {
  currentPage.value = 1
  fetchContracts()
}

function getApprovalText(status) {
  const map = { 0: '待审核', 1: '已通过', 2: '已拒绝' }
  return map[status] ?? '未知'
}

function getContractStatusText(status) {
  const map = { 0: '初始', 1: '进行中', 2: '已完成', 3: '已终止' }
  return map[status] ?? '未知'
}

function handleReset() {
  searchForm.value = {
    name: '',
    number: '',
    startDate: '',
    endDate: '',
    approvalStatus: '',
    contractStatus: ''
  }
  currentPage.value = 1
  fetchContracts()
}

// 翻页
function goToPage(page) {
  if (page < 1 || page > totalPages.value) return
  currentPage.value = page
  fetchContracts()
}

function viewContract(id) {
  router.push(`/contracts/${id}`)
}

function editContract(id) {
  router.push(`/contracts/edit/${id}`)
}

async function previewFile(id, filename) {
  try {
    const response = await axios.get(`/contracts/${id}/download`, {
      responseType: 'blob'
    })
    const blob = new Blob([response.data])
    const url = window.URL.createObjectURL(blob)
    window.open(url, '_blank')
    // 延迟释放，给浏览器时间加载
    setTimeout(() => window.URL.revokeObjectURL(url), 10000)
  } catch (err) {
    alert('预览失败：' + (err.response?.data?.message || err.message))
  }
}

async function downloadFile(id, filename) {
  try {
    const response = await axios.get(`/contracts/${id}/download`, {
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

function getRowClass(contract) {
  if (contract.isFullyPaid) return 'row-completed'
  return ''
}

function getProgress(contract) {
  if (contract.totalAmount === 0) return 0
  return Math.round((contract.paidAmount / contract.totalAmount) * 100)
}

function formatAmount(amount) {
  return amount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

async function deleteContract(id, name) {
  if (!confirm(`确定要删除合同「${name}」吗？此操作不可恢复。`)) return
  try {
    await axios.delete(`/contracts/${id}`)
    await fetchContracts()
  } catch (err) {
    alert('删除失败：' + (err.response?.data?.message || err.message))
  }
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
  max-width: 1600px;
  margin: 0 auto;
  padding: 0 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
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

/* 查询面板 */
.search-panel {
  background: white;
  border-radius: 12px;
  padding: 20px;
  margin-bottom: 24px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.search-group {
  display: grid;
  grid-template-columns: 1fr 1fr 2fr 1fr 1fr auto;
  gap: 16px;
  align-items: flex-end;
}

.form-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-item label {
  font-weight: 600;
  color: #2c3e50;
  font-size: 13px;
}

.form-item input {
  padding: 10px 12px;
  border: 2px solid #ecf0f1;
  border-radius: 6px;
  font-size: 14px;
  transition: border-color 0.3s;
}

.form-item input:focus {
  outline: none;
  border-color: #667eea;
}

.date-range {
  display: flex;
  align-items: center;
  gap: 8px;
}

.date-range input {
  flex: 1;
  padding: 10px 12px;
  border: 2px solid #ecf0f1;
  border-radius: 6px;
  font-size: 14px;
}

.date-range input:focus {
  outline: none;
  border-color: #667eea;
}

.separator {
  color: #95a5a6;
  font-weight: 600;
}

.form-actions {
  display: flex;
  gap: 8px;
}

.search-btn,
.reset-btn {
  flex: 1;
  padding: 10px 16px;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.search-btn {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  box-shadow: 0 2px 8px rgba(102, 126, 234, 0.2);
}

.search-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
}

.reset-btn {
  background: #ecf0f1;
  color: #2c3e50;
}

.reset-btn:hover {
  background: #bdc3c7;
}

/* 加载/错误/空状态 */
.loading,
.error,
.empty {
  text-align: center;
  padding: 60px 20px;
  font-size: 16px;
  color: #7f8c8d;
  background: white;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.error {
  color: #e74c3c;
}

/* 表格容器 */
.table-container {
  background: white;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.contracts-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
}

.contracts-table thead {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.contracts-table th {
  padding: 16px 12px;
  text-align: left;
  font-weight: 600;
  letter-spacing: 0.5px;
}

.contracts-table tbody tr {
  border-bottom: 1px solid #ecf0f1;
  transition: background-color 0.2s;
}

.contracts-table tbody tr:hover {
  background-color: #f8f9fa;
}

.contracts-table tbody tr.row-completed {
  background-color: rgba(39, 174, 96, 0.05);
}

.contracts-table td {
  padding: 14px 12px;
  color: #2c3e50;
}

.cell-id {
  font-weight: 600;
  color: #667eea;
  max-width: 60px;
}

.cell-number {
  font-family: monospace;
  font-weight: 500;
  color: #34495e;
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.cell-name {
  font-weight: 500;
  color: #2c3e50;
  max-width: 200px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.cell-amount,
.cell-paid {
  font-weight: 600;
  text-align: right;
  min-width: 100px;
}

.cell-amount {
  color: #3498db;
}

.cell-paid {
  color: #27ae60;
}

.cell-progress {
  min-width: 140px;
}

.progress-container {
  display: flex;
  align-items: center;
  gap: 8px;
}

.progress-bar {
  flex: 1;
  height: 6px;
  background: #ecf0f1;
  border-radius: 3px;
  overflow: hidden;
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
  font-size: 12px;
  font-weight: 600;
  color: #7f8c8d;
  min-width: 32px;
  text-align: right;
}

.form-item select {
  padding: 10px 12px;
  border: 2px solid #ecf0f1;
  border-radius: 6px;
  font-size: 14px;
  background: white;
}

.approval-badge {
  display: inline-block;
  padding: 4px 10px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}
.approval-0 { background: #fff3cd; color: #856404; }
.approval-1 { background: #d4edda; color: #155724; }
.approval-2 { background: #f8d7da; color: #721c24; }

.contract-status-badge { display: inline-block; padding: 4px 10px; border-radius: 12px; font-size: 12px; font-weight: 600; }
.cs-0 { background: #e2e3e5; color: #383d41; }
.cs-1 { background: #cce5ff; color: #004085; }
.cs-2 { background: #d4edda; color: #155724; }
.cs-3 { background: #f5c6cb; color: #721c24; }

.cell-date {
  color: #7f8c8d;
  font-size: 13px;
  min-width: 140px;
  white-space: nowrap;
}

.cell-status {
  white-space: nowrap;
  width: 1%;
}

.cell-actions {
  text-align: center;
  min-width: 150px;
}

.action-buttons {
  display: flex;
  gap: 6px;
  justify-content: center;
  flex-wrap: wrap;
}

.action-btn {
  width: 32px;
  height: 32px;
  padding: 0;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  font-size: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.detail-btn {
  background: #667eea;
  color: white;
}

.detail-btn:hover {
  background: #5568d3;
  transform: scale(1.1);
}

.edit-btn {
  background: #f39c12;
  color: white;
}

.edit-btn:hover {
  background: #e67e22;
  transform: scale(1.1);
}

.preview-btn {
  background: #3498db;
  color: white;
}

.preview-btn:hover {
  background: #2980b9;
  transform: scale(1.1);
}

.download-btn {
  background: #27ae60;
  color: white;
}

.download-btn:hover {
  background: #229954;
  transform: scale(1.1);
}

.delete-btn {
  background: #e74c3c;
  color: white;
}

.delete-btn:hover {
  background: #c0392b;
  transform: scale(1.1);
}

/* 分页 */
.pagination {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-top: 1px solid #ecf0f1;
}

.pagination-info {
  color: #7f8c8d;
  font-size: 14px;
}

.pagination-buttons {
  display: flex;
  gap: 8px;
}

.page-btn {
  padding: 8px 18px;
  background: #f5f7fa;
  color: #34495e;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s;
}

.page-btn:hover:not(:disabled) {
  background: #667eea;
  color: white;
  border-color: #667eea;
}

.page-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

/* 响应式设计 */
@media (max-width: 1200px) {
  .contracts-table {
    font-size: 12px;
  }

  .contracts-table th,
  .contracts-table td {
    padding: 10px 8px;
  }

  .action-buttons {
    gap: 4px;
  }

  .action-btn {
    width: 28px;
    height: 28px;
    font-size: 12px;
  }
}

@media (max-width: 768px) {
  .search-group {
    grid-template-columns: 1fr;
  }

  .form-actions {
    grid-column: 1 / -1;
  }

  .page-header {
    flex-direction: column;
    align-items: stretch;
    gap: 12px;
  }

  .add-btn {
    text-align: center;
  }

  .contracts-table {
    font-size: 11px;
  }

  .contracts-table th,
  .contracts-table td {
    padding: 8px 6px;
  }

  .action-buttons {
    gap: 2px;
  }

  .action-btn {
    width: 24px;
    height: 24px;
    font-size: 11px;
  }

  .cell-name {
    max-width: 100px;
  }

  .cell-number {
    max-width: 80px;
  }

  .pagination {
    flex-direction: column;
    gap: 12px;
    text-align: center;
  }
}
</style>
