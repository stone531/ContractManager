<template>
  <div class="contract-add-view">
    <div class="page-header">
      <h2>增加合同</h2>
      <router-link to="/contracts/list" class="back-btn">
        ← 返回列表
      </router-link>
    </div>

    <div class="form-container">
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

        <div class="form-group">
          <label for="totalAmount">合同总金额 <span class="required">*</span></label>
          <div class="amount-input">
            <span class="currency">¥</span>
            <input
              id="totalAmount"
              v-model.number="form.totalAmount"
              type="number"
              step="0.01"
              min="0"
              placeholder="0.00"
              required
            />
          </div>
        </div>

        <div class="form-group">
          <label for="file">合同文件</label>
          <div class="file-upload">
            <input
              id="file"
              type="file"
              @change="handleFileChange"
              accept=".pdf,.doc,.docx,.jpg,.jpeg,.png"
              ref="fileInput"
            />
            <div class="file-upload-area" @click="$refs.fileInput.click()">
              <div v-if="!selectedFile" class="upload-placeholder">
                <span class="upload-icon">📤</span>
                <p>点击选择文件或拖拽文件到此处</p>
                <p class="file-tip">支持格式：PDF, DOC, DOCX, JPG, PNG</p>
              </div>
              <div v-else class="file-selected">
                <span class="file-icon">📄</span>
                <div class="file-info">
                  <p class="file-name">{{ selectedFile.name }}</p>
                  <p class="file-size">{{ formatFileSize(selectedFile.size) }}</p>
                </div>
                <button type="button" @click.stop="clearFile" class="clear-file">✕</button>
              </div>
            </div>
          </div>
        </div>

        <div class="form-actions">
          <button type="submit" class="submit-btn" :disabled="submitting">
            <span v-if="submitting">上传中...</span>
            <span v-else>📤 提交</span>
          </button>
          <button type="button" @click="handleReset" class="reset-btn" :disabled="submitting">
            🔄 重置
          </button>
        </div>

        <div v-if="error" class="error-message">{{ error }}</div>
        <div v-if="success" class="success-message">{{ success }}</div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from '../api/axios'

const router = useRouter()
const fileInput = ref(null)

const form = ref({
  name: '',
  description: '',
  totalAmount: 0
})

const selectedFile = ref(null)
const submitting = ref(false)
const error = ref(null)
const success = ref(null)

function handleFileChange(event) {
  const file = event.target.files[0]
  if (file) {
    selectedFile.value = file
  }
}

function clearFile() {
  selectedFile.value = null
  if (fileInput.value) {
    fileInput.value.value = ''
  }
}

function formatFileSize(bytes) {
  if (bytes === 0) return '0 Bytes'
  const k = 1024
  const sizes = ['Bytes', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
}

async function handleSubmit() {
  error.value = null
  success.value = null
  submitting.value = true

  try {
    const formData = new FormData()
    formData.append('name', form.value.name)
    formData.append('description', form.value.description || '')
    formData.append('totalAmount', form.value.totalAmount)
    
    if (selectedFile.value) {
      formData.append('file', selectedFile.value)
    }

    await axios.post('/api/contracts', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })

    success.value = '合同创建成功！即将跳转...'
    
    setTimeout(() => {
      router.push('/contracts/list')
    }, 1500)
  } catch (err) {
    error.value = '创建失败：' + (err.response?.data?.message || err.message)
  } finally {
    submitting.value = false
  }
}

function handleReset() {
  form.value = {
    name: '',
    description: '',
    totalAmount: 0
  }
  clearFile()
  error.value = null
  success.value = null
}
</script>

<style scoped>
.contract-add-view {
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

.amount-input {
  position: relative;
  display: flex;
  align-items: center;
}

.currency {
  position: absolute;
  left: 16px;
  font-weight: 600;
  color: #7f8c8d;
  font-size: 16px;
}

.amount-input input {
  padding: 12px 16px 12px 40px;
  border: 2px solid #ecf0f1;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 600;
  width: 100%;
  transition: border-color 0.3s;
}

.amount-input input:focus {
  outline: none;
  border-color: #667eea;
}

.file-upload input[type="file"] {
  display: none;
}

.file-upload-area {
  border: 2px dashed #bdc3c7;
  border-radius: 8px;
  padding: 32px;
  text-align: center;
  cursor: pointer;
  transition: all 0.3s;
  background: #f8f9fa;
}

.file-upload-area:hover {
  border-color: #667eea;
  background: #f0f1ff;
}

.upload-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}

.upload-icon {
  font-size: 48px;
}

.upload-placeholder p {
  margin: 0;
  color: #7f8c8d;
}

.file-tip {
  font-size: 12px;
  color: #95a5a6;
}

.file-selected {
  display: flex;
  align-items: center;
  gap: 16px;
  background: white;
  padding: 16px;
  border-radius: 6px;
}

.file-icon {
  font-size: 32px;
}

.file-info {
  flex: 1;
  text-align: left;
}

.file-name {
  margin: 0;
  font-weight: 600;
  color: #2c3e50;
}

.file-size {
  margin: 4px 0 0 0;
  font-size: 12px;
  color: #95a5a6;
}

.clear-file {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: none;
  background: #e74c3c;
  color: white;
  cursor: pointer;
  transition: all 0.3s;
  font-size: 16px;
}

.clear-file:hover {
  background: #c0392b;
  transform: scale(1.1);
}

.form-actions {
  display: flex;
  gap: 12px;
  margin-top: 16px;
}

.submit-btn,
.reset-btn {
  flex: 1;
  padding: 14px 24px;
  border: none;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
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

.reset-btn {
  background: #ecf0f1;
  color: #2c3e50;
}

.reset-btn:hover:not(:disabled) {
  background: #bdc3c7;
}

.reset-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
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
}
</style>
