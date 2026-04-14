<template>
  <div class="page-content">
    <div class="page-header">
      <h2>新增用户</h2>
      <p class="subtitle">填写用户信息以创建新用户</p>
    </div>

    <div class="form-card">
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
          <span v-if="loading">提交中...</span>
          <span v-else>✓ 提交</span>
        </button>
        <button @click="handleReset" class="btn-secondary" :disabled="loading">
          ↺ 重置
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
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import apiClient from '../api/axios'

const router = useRouter()
const form = ref({ name: '', email: '' })
const loading = ref(false)
const successMessage = ref('')
const errorMessage = ref('')

async function handleSubmit() {
  if (!form.value.name || !form.value.email) {
    errorMessage.value = '请填写所有必填字段'
    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await apiClient.post('/users', form.value)
    successMessage.value = `用户 "${form.value.name}" 添加成功！`
    form.value = { name: '', email: '' }
    
    // 3秒后自动跳转到用户列表
    setTimeout(() => {
      router.push('/users/list')
    }, 2000)
  } catch (error) {
    errorMessage.value = '添加失败: ' + (error.response?.data?.message || error.message)
  } finally {
    loading.value = false
  }
}

function handleReset() {
  form.value = { name: '', email: '' }
  errorMessage.value = ''
  successMessage.value = ''
}
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
</style>
