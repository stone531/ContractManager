import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authAPI } from '../api/auth'

export const useAuthStore = defineStore('auth', () => {
  const user = ref(null)
  const token = ref(null)

  const isAuthenticated = computed(() => !!token.value)

  // 从 localStorage 恢复登录状态
  function loadFromStorage() {
    const storedToken = localStorage.getItem('token')
    const storedUser = localStorage.getItem('user')

    if (storedToken && storedUser) {
      token.value = storedToken
      user.value = JSON.parse(storedUser)
    }
  }

  // 用户登录
  async function login(email, password) {
    try {
      const response = await authAPI.login(email, password)
      const { token: newToken, user: newUser } = response.data

      // 保存到状态
      token.value = newToken
      user.value = newUser

      // 保存到 localStorage
      localStorage.setItem('token', newToken)
      localStorage.setItem('user', JSON.stringify(newUser))

      return { success: true }
    } catch (error) {
      return {
        success: false,
        message: error.response?.data?.message || '登录失败，请重试'
      }
    }
  }

  // 用户注册
  async function register(name, email, password) {
    try {
      const response = await authAPI.register(name, email, password)
      const { token: newToken, user: newUser } = response.data

      // 保存到状态
      token.value = newToken
      user.value = newUser

      // 保存到 localStorage
      localStorage.setItem('token', newToken)
      localStorage.setItem('user', JSON.stringify(newUser))

      return { success: true }
    } catch (error) {
      return {
        success: false,
        message: error.response?.data?.message || '注册失败，请重试'
      }
    }
  }

  // 用户注销
  function logout() {
    token.value = null
    user.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  return {
    user,
    token,
    isAuthenticated,
    login,
    register,
    logout,
    loadFromStorage
  }
})
