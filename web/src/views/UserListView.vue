<template>
  <div class="page-content">
    <div class="page-header">
      <h2>查询用户</h2>
      <p class="subtitle">查看和管理所有用户信息</p>
    </div>

    <!-- 搜索框 -->
    <div class="search-bar">
      <input
        v-model="searchQuery"
        type="text"
        placeholder="🔍 搜索用户名或邮箱..."
        class="search-input"
        @keyup.enter="handleSearch"
      />
      <button @click="handleSearch" class="refresh-btn" :disabled="loading">
        🔍 搜索
      </button>
      <button @click="handleReset" class="refresh-btn" :disabled="loading">
        🔄 重置
      </button>
    </div>

    <!-- 加载状态 -->
    <div v-if="loading" class="loading">
      <div class="spinner"></div>
      <p>加载中...</p>
    </div>

    <!-- 错误提示 -->
    <div v-if="errorMessage" class="alert alert-error">
      {{ errorMessage }}
      <button @click="errorMessage = ''" class="alert-close">×</button>
    </div>

    <!-- 用户列表 -->
    <div v-if="!loading" class="table-card">
      <div class="table-header">
        <h3>用户列表 ({{ totalCount }})</h3>
      </div>
      
      <div v-if="users.length" class="table-wrapper">
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>姓名</th>
              <th>邮箱</th>
              <th>角色</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="user in users" :key="user.id">
              <td>{{ user.id }}</td>
              <td>
                <div class="user-name">{{ user.name }}</div>
              </td>
              <td>{{ user.email }}</td>
              <td>
                <span class="role-badge" :class="isUserAdmin(user) ? 'role-admin' : 'role-user'">
                  {{ isUserAdmin(user) ? '超级管理员' : '普通用户' }}
                </span>
              </td>
              <td>{{ formatDate(user.createdAt) }}</td>
              <td>
                <div class="action-buttons">
                  <button
                    class="btn-edit"
                    @click="editUser(user.id)"
                    title="编辑用户"
                  >
                    ✏️ 编辑
                  </button>
                  <button
                    v-if="isSuperAdmin"
                    class="btn-delete"
                    :class="{ 'btn-disabled': isUserAdmin(user) }"
                    :disabled="isUserAdmin(user)"
                    @click="deleteUser(user.id, user.name)"
                    :title="isUserAdmin(user) ? '不能删除管理员' : '删除用户'"
                  >
                    🗑️ 删除
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-else class="empty-state">
        <div class="empty-icon">📭</div>
        <p>暂无用户数据</p>
        <router-link to="/users/add" class="btn-add-user">
          + 添加第一个用户
        </router-link>
      </div>

      <!-- 分页 -->
      <div v-if="users.length" class="pagination">
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
import apiClient from '../api/axios'

const router = useRouter()
const authStore = useAuthStore()

const isSuperAdmin = computed(() => {
  const role = authStore.user?.role
  return role === 0 || role === 'SuperAdmin'
})
const users = ref([])
const loading = ref(false)
const errorMessage = ref('')
const searchQuery = ref('')

// 分页状态
const currentPage = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

const totalPages = computed(() => {
  return Math.max(1, Math.ceil(totalCount.value / pageSize.value))
})

// 获取用户列表
async function fetchUsers() {
  loading.value = true
  errorMessage.value = ''
  
  try {
    const res = await apiClient.get('/users', {
      params: {
        search: searchQuery.value || undefined,
        page: currentPage.value,
        pageSize: pageSize.value
      }
    })
    users.value = res.data.items
    totalCount.value = res.data.total
  } catch (error) {
    errorMessage.value = '获取数据失败: ' + (error.response?.data?.message || error.message)
  } finally {
    loading.value = false
  }
}

// 搜索
function handleSearch() {
  currentPage.value = 1
  fetchUsers()
}

// 重置
function handleReset() {
  searchQuery.value = ''
  currentPage.value = 1
  fetchUsers()
}

// 翻页
function goToPage(page) {
  if (page < 1 || page > totalPages.value) return
  currentPage.value = page
  fetchUsers()
}

// 编辑用户
function editUser(id) {
  router.push(`/users/edit/${id}`)
}

// 删除用户
async function deleteUser(id, name) {
  if (!confirm(`确认删除用户 "${name}" 吗？此操作不可恢复。`)) return
  
  try {
    await apiClient.delete(`/users/${id}`)
    await fetchUsers()
  } catch (error) {
    errorMessage.value = '删除失败: ' + (error.response?.data?.message || error.message)
  }
}

// 判断用户是否为管理员
function isUserAdmin(user) {
  return user.role === 0 || user.role === 'SuperAdmin'
}

// 格式化日期
function formatDate(dateString) {
  const date = new Date(dateString)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

onMounted(fetchUsers)
</script>

<style scoped>
.page-content {
  max-width: 1200px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 24px;
}

.page-header h2 {
  color: #2c3e50;
  font-size: 28px;
  font-weight: 600;
  margin-bottom: 8px;
}

.subtitle {
  color: #7f8c8d;
  font-size: 14px;
  margin: 0;
}

.search-bar {
  display: flex;
  gap: 12px;
  margin-bottom: 24px;
}

.search-input {
  flex: 1;
  padding: 12px 16px;
  border: 2px solid #e0e0e0;
  border-radius: 8px;
  font-size: 15px;
  transition: all 0.3s;
}

.search-input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.refresh-btn {
  padding: 12px 20px;
  background: #f5f7fa;
  color: #34495e;
  border: none;
  border-radius: 8px;
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.refresh-btn:hover:not(:disabled) {
  background: #e8eaed;
}

.refresh-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.loading {
  text-align: center;
  padding: 60px 20px;
}

.spinner {
  width: 48px;
  height: 48px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #667eea;
  border-radius: 50%;
  margin: 0 auto 16px;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.alert {
  margin-bottom: 24px;
  padding: 14px 16px;
  border-radius: 8px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  animation: slideIn 0.3s ease;
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.alert-error {
  background: #f8d7da;
  color: #721c24;
  border: 1px solid #f5c6cb;
}

.alert-close {
  background: none;
  border: none;
  font-size: 24px;
  color: inherit;
  cursor: pointer;
  padding: 0;
  width: 24px;
  height: 24px;
  line-height: 1;
  opacity: 0.6;
}

.alert-close:hover {
  opacity: 1;
}

.table-card {
  background: white;
  border-radius: 12px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  overflow: hidden;
}

.table-header {
  padding: 20px 24px;
  border-bottom: 1px solid #f0f0f0;
}

.table-header h3 {
  margin: 0;
  color: #2c3e50;
  font-size: 18px;
  font-weight: 600;
}

.table-wrapper {
  overflow-x: auto;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th, td {
  padding: 16px 24px;
  text-align: left;
}

th {
  background: #f8f9fa;
  color: #2c3e50;
  font-weight: 600;
  font-size: 14px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  border-bottom: 2px solid #e9ecef;
}

td {
  border-bottom: 1px solid #f0f0f0;
  color: #34495e;
}

tbody tr {
  transition: background 0.2s;
}

tbody tr:hover {
  background: #f8f9fa;
}

.user-name {
  font-weight: 500;
}

.action-buttons {
  display: flex;
  gap: 8px;
}

.btn-edit {
  padding: 6px 14px;
  background: #e3f2fd;
  color: #1976d2;
  border: 1px solid #bbdefb;
  border-radius: 6px;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-edit:hover {
  background: #1976d2;
  color: white;
  border-color: #1976d2;
}

.btn-delete {
  padding: 6px 14px;
  background: #fff5f5;
  color: #e74c3c;
  border: 1px solid #ffdddd;
  border-radius: 6px;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-delete:hover:not(:disabled) {
  background: #e74c3c;
  color: white;
  border-color: #e74c3c;
}

.btn-delete.btn-disabled {
  background: #f0f0f0;
  color: #bbb;
  border-color: #e0e0e0;
  cursor: not-allowed;
  opacity: 0.6;
}

/* 角色标签 */
.role-badge {
  display: inline-block;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
  white-space: nowrap;
}

.role-admin {
  background: #fff3cd;
  color: #856404;
  border: 1px solid #ffeeba;
}

.role-user {
  background: #d4edda;
  color: #155724;
  border: 1px solid #c3e6cb;
}

.empty-state {
  text-align: center;
  padding: 80px 20px;
}

.empty-icon {
  font-size: 64px;
  margin-bottom: 16px;
}

.empty-state p {
  color: #7f8c8d;
  font-size: 16px;
  margin-bottom: 24px;
}

.btn-add-user {
  display: inline-block;
  padding: 12px 24px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  text-decoration: none;
  border-radius: 8px;
  font-weight: 600;
  transition: all 0.3s;
}

.btn-add-user:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

/* 分页 */
.pagination {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 24px;
  border-top: 1px solid #f0f0f0;
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
@media (max-width: 768px) {
  .table-wrapper {
    overflow-x: auto;
  }
  
  th, td {
    padding: 12px 16px;
    font-size: 14px;
  }
  
  .btn-edit,
  .btn-delete {
    padding: 4px 10px;
    font-size: 12px;
  }
  
  .action-buttons {
    flex-direction: column;
    gap: 4px;
  }

  .pagination {
    flex-direction: column;
    gap: 12px;
    text-align: center;
  }
}
</style>
