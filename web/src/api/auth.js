import apiClient from './axios'

export const authAPI = {
  // 用户注册
  register(username, name, email, password) {
    return apiClient.post('/auth/register', { username, name, email, password })
  },

  // 用户登录
  login(username, password) {
    return apiClient.post('/auth/login', { username, password })
  }
}
