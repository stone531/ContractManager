<template>
  <div class="page-content">
    <div class="page-header">
      <h2>编辑用户</h2>
      <p class="subtitle">修改用户信息</p>
    </div>

    <!-- 加载状态 -->
    <div v-if="loadingUser" class="loading">
      <div class="spinner"></div>
      <p>加载用户信息...</p>
    </div>

    <div v-else class="form-card">
      <div class="form-group">
        <label for="name">姓名 <span class="required">*</span></label>
        <input
          id="name"
          v-model="form.name"
          type="text"
          placeholder="请输入姓名"
          @keyup.enter="handleSubmit"
        />
      </div>

      <div class="form-group">
        <label for="email">邮箱 <span class="required">*</span></label>
        <input
          id="email"
          v-model="form.email"
          type="email"
          placeholder="请输入邮箱地址"
          @keyup.enter="handleSubmit"
        />
      </div>

      <div class="form-actions">
        <button
          @click="handleSubmit"
          :disabled="!form.name || !form.email || loading"
          class="btn-primary"
        >
          <span v-if="loading">保存中...</span>
          <span v-else>✓ 保存</span>
        </button>
        <button @click="handleCancel" class="btn-secondary" :disabled="loading">
          ← 取消
        </button>
      </div>

      <!-- 成功提示 -->
      <div v-if="successMessage" class="alert alert-success">
        {{ successMessage }}
        <button @click="successMessage = ''" class="alert-close">×</button>
      </div>

      <!-- 错误提示 -->
      <div v-if="errorMessage" class="alert alert-error">
        {{ errorMessage }}
        <button @click="errorMessage = ''" class="alert-close">×</button>
      </div>
    </div>

    <!-- 管理员重置密码区域 -->
    <div v-if="!loadingUser && isSuperAdmin" class="form-card reset-password-card">
      <h3 class="section-title">🔑 重置用户密码</h3>
      <p class="section-desc">为该用户设置新密码（最少6位）</p>

      <div class="form-group">
        <label for="newPassword">新密码 <span class="required">*</span></label>
        <input
          id="newPassword"
          v-model="resetForm.newPassword"
          type="password"
          placeholder="请输入新密码（至少6位）"
        />
      </div>

      <div class="form-group">
        <label for="confirmPassword">确认密码 <span class="required">*</span></label>
        <input
          id="confirmPassword"
          v-model="resetForm.confirmPassword"
          type="password"
          placeholder="请再次输入新密码"
          @keyup.enter="handleResetPassword"
        />
      </div>

      <div class="form-actions">
        <button
          @click="handleResetPassword"
          :disabled="!resetForm.newPassword || !resetForm.confirmPassword || resetting"
          class="btn-warning"
        >
          <span v-if="resetting">重置中...</span>
          <span v-else>🔑 重置密码</span>
        </button>
      </div>

      <div v-if="resetSuccess" class="alert alert-success">
        {{ resetSuccess }}
        <button @click="resetSuccess = ''" class="alert-close">×</button>
      </div>
      <div v-if="resetError" class="alert alert-error">
        {{ resetError }}
        <button @click="resetError = ''" class="alert-close">×</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import apiClient from '../api/axios'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const userId = ref(route.params.id)

const isSuperAdmin = computed(() => {
  const role = authStore.user?.role
  return role === 0 || role === 'SuperAdmin'
})

const form = ref({ id: 0, name: '', email: '' })
const loading = ref(false)
const loadingUser = ref(false)
const successMessage = ref('')
const errorMessage = ref('')

// 重置密码相关
const resetForm = ref({ newPassword: '', confirmPassword: '' })
const resetting = ref(false)
const resetSuccess = ref('')
const resetError = ref('')

// 加载用户数据
async function loadUser() {
  loadingUser.value = true
  errorMessage.value = ''

  try {
    const res = await apiClient.get(`/users/${userId.value}`)
    form.value = { ...res.data }
  } catch (error) {
    errorMessage.value = '加载用户失败: ' + (error.response?.data?.message || error.message)
  } finally {
    loadingUser.value = false
  }
}

// 提交更新
async function handleSubmit() {
  if (!form.value.name || !form.value.email) {
    errorMessage.value = '请填写所有必填字段'
    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await apiClient.put(`/users/${userId.value}`, form.value)
    successMessage.value = `用户 "${form.value.name}" 更新成功！`
    
    // 1.5秒后自动跳转到用户列表
    setTimeout(() => {
      router.push('/users/list')
    }, 1500)
  } catch (error) {
    errorMessage.value = '更新失败: ' + (error.response?.data?.message || error.message)
  } finally {
    loading.value = false
  }
}

// 管理员重置密码
async function handleResetPassword() {
  resetSuccess.value = ''
  resetError.value = ''

  if (resetForm.value.newPassword.length < 6) {
    resetError.value = '密码至少需要 6 个字符'
    return
  }
  if (resetForm.value.newPassword !== resetForm.value.confirmPassword) {
    resetError.value = '两次输入的密码不一致'
    return
  }

  resetting.value = true
  try {
    const res = await apiClient.put(`/users/${userId.value}/reset-password`, {
      newPassword: resetForm.value.newPassword
    })
    resetSuccess.value = res.data.message || '密码重置成功'
    resetForm.value = { newPassword: '', confirmPassword: '' }
  } catch (error) {
    resetError.value = '重置失败: ' + (error.response?.data?.message || error.message)
  } finally {
    resetting.value = false
  }
}

// 取消编辑
function handleCancel() {
  router.push('/users/list')
}

onMounted(loadUser)
</script>

<style scoped>
.page-content {
  max-width: 800px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 32px;
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

.form-card {
  background: white;
  border-radius: 12px;
  padding: 32px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
}

.form-group {
  margin-bottom: 24px;
}

.form-group label {
  display: block;
  margin-bottom: 8px;
  color: #2c3e50;
  font-weight: 500;
  font-size: 14px;
}

.required {
  color: #e74c3c;
}

.form-group input {
  width: 100%;
  padding: 12px 16px;
  border: 2px solid #e0e0e0;
  border-radius: 8px;
  font-size: 15px;
  transition: all 0.3s;
}

.form-group input:focus {
  outline: none;
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.form-group input::placeholder {
  color: #bdc3c7;
}

.form-actions {
  display: flex;
  gap: 12px;
  margin-top: 32px;
}

button {
  padding: 12px 24px;
  border: none;
  border-radius: 8px;
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-primary {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  flex: 1;
}

.btn-primary:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.btn-secondary {
  background: #f5f7fa;
  color: #34495e;
  padding: 12px 20px;
}

.btn-secondary:hover:not(:disabled) {
  background: #e8eaed;
}

.btn-secondary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.alert {
  margin-top: 24px;
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

.alert-success {
  background: #d4edda;
  color: #155724;
  border: 1px solid #c3e6cb;
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

/* 重置密码区域 */
.reset-password-card {
  margin-top: 24px;
  border-top: 3px solid #f39c12;
}

.section-title {
  margin: 0 0 8px;
  color: #2c3e50;
  font-size: 20px;
  font-weight: 600;
}

.section-desc {
  color: #7f8c8d;
  font-size: 14px;
  margin: 0 0 24px;
}

.btn-warning {
  background: linear-gradient(135deg, #f39c12 0%, #e67e22 100%);
  color: white;
  flex: 1;
}

.btn-warning:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(243, 156, 18, 0.4);
}

.btn-warning:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}
</style>
