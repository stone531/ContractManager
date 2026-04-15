<template>
  <div class="page-container">
    <!-- 导航栏 -->
    <nav class="navbar">
      <h1>用户管理系统</h1>
      <div class="user-info">
        <span>欢迎, {{ authStore.user?.name }}</span>
        <button @click="handleLogout" class="logout-btn">注销</button>
      </div>
    </nav>

    <!-- 主内容 -->
    <div class="container">
      <!-- 新增用户表单 -->
      <div class="form">
        <h2>新增用户</h2>
        <input v-model="form.name" placeholder="姓名" />
        <input v-model="form.email" placeholder="邮箱" />
        <button @click="addUser" :disabled="!form.name || !form.email">添加</button>
      </div>

      <!-- 用户列表 -->
      <div class="table-wrap">
        <h2>用户列表</h2>
        <p v-if="loading">加载中...</p>
        <p v-if="error" class="error">{{ error }}</p>
        <table v-if="!loading && users.length">
          <thead>
            <tr>
              <th>ID</th>
              <th>用户名</th>
              <th>姓名</th>
              <th>邮箱</th>
              <th>角色</th>
              <th>状态</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="user in users" :key="user.id">
              <td>{{ user.id }}</td>
              <td>{{ user.userName }}</td>
              <td>{{ user.name }}</td>
              <td>{{ user.email }}</td>
              <td>{{ getRoleLabel(user.role) }}</td>
              <td>{{ user.isEnabled ? '启用' : '禁用' }}</td>
              <td>{{ new Date(user.createdAt).toLocaleString() }}</td>
              <td>
                <button 
                  class="del" 
                  @click="deleteUser(user.id)"
                  :disabled="isCannotDelete(user)"
                  :title="isCannotDelete(user) ? '超级管理员无法删除自己' : '删除'"
                >
                  删除
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        <p v-if="!loading && !users.length">暂无用户</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import apiClient from '../api/axios'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const users = ref([])
const loading = ref(false)
const error = ref('')
const form = ref({ name: '', email: '' })

async function fetchUsers() {
  loading.value = true
  error.value = ''
  try {
    const res = await apiClient.get('/users')
    users.value = res.data
  } catch (e) {
    error.value = '获取数据失败: ' + (e.response?.data?.message || e.message)
  } finally {
    loading.value = false
  }
}

async function addUser() {
  try {
    await apiClient.post('/users', form.value)
    form.value = { name: '', email: '' }
    await fetchUsers()
  } catch (e) {
    error.value = '添加失败: ' + (e.response?.data?.message || e.message)
  }
}

async function deleteUser(id) {
  if (!confirm('确认删除？')) return
  try {
    await apiClient.delete(`/users/${id}`)
    await fetchUsers()
  } catch (e) {
    error.value = '删除失败: ' + (e.response?.data?.message || e.message)
  }
}

function getRoleLabel(role) {
  const labels = { 0: 'SuperAdmin', 1: 'Admin', 2: 'User' }
  return labels[role] || 'Unknown'
}

function isCannotDelete(user) {
  // 当前用户是 SuperAdmin 且是同一用户，则不能删除
  return authStore.user?.id === user.id && user.role === 0
}

function handleLogout() {
  authStore.logout()
  router.push('/login')
}

onMounted(fetchUsers)
</script>

<style scoped>
.page-container {
  min-height: 100vh;
  background: #f5f7fa;
}

.navbar {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 16px 24px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.navbar h1 {
  margin: 0;
  font-size: 24px;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 16px;
}

.logout-btn {
  padding: 8px 16px;
  background: rgba(255, 255, 255, 0.2);
  color: white;
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 6px;
  cursor: pointer;
  transition: background 0.3s;
}

.logout-btn:hover {
  background: rgba(255, 255, 255, 0.3);
}

.container {
  max-width: 900px;
  margin: 40px auto;
  padding: 0 20px;
}

h2 {
  color: #34495e;
  font-size: 1.2rem;
  margin-bottom: 16px;
}

.form {
  background: white;
  padding: 24px;
  border-radius: 12px;
  margin-bottom: 32px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  display: flex;
  gap: 12px;
  align-items: center;
  flex-wrap: wrap;
}

input {
  padding: 10px 14px;
  border: 2px solid #e0e0e0;
  border-radius: 8px;
  font-size: 14px;
  flex: 1;
  min-width: 150px;
  transition: border-color 0.3s;
}

input:focus {
  outline: none;
  border-color: #667eea;
}

button {
  padding: 10px 20px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 600;
  transition: opacity 0.3s;
}

button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

button:hover:not(:disabled) {
  opacity: 0.9;
}

button.del {
  background: #e74c3c;
  padding: 6px 14px;
  font-size: 13px;
}

button.del:hover:not(:disabled) {
  background: #c0392b;
}

button.del:disabled {
  background: #bdc3c7;
  cursor: not-allowed;
  opacity: 0.6;
}

.table-wrap {
  background: white;
  padding: 24px;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

table {
  width: 100%;
  border-collapse: collapse;
}

th, td {
  padding: 14px;
  text-align: left;
  border-bottom: 1px solid #f0f0f0;
}

th {
  background: #f8f9fa;
  color: #2c3e50;
  font-weight: 600;
}

tr:hover {
  background: #f8f9fa;
}

.error {
  color: #e74c3c;
  font-size: 14px;
}
</style>
