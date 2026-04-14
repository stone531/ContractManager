import apiClient from './axios'

export const authAPI = {
  // 用户注册
  register(name, email, password) {
    return apiClient.post('/auth/register', { name, email, password })
  },

  // 用户登录
  login(email, password) {
    return apiClient.post('/auth/login', { email, password })
  }
}
