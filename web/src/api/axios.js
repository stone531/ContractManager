import axios from 'axios'

//创建 axios 实例
const apiClient = axios.create({
  baseURL: '/api',
  withCredentials: true
})

// 请求拦截器：自动附加 JWT token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// 响应拦截器：处理 401 错误
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // 未授权，清除 token 并重定向到登录页
      localStorage.removeItem('token')
     localStorage.removeItem('user')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export default apiClient
