import axios from 'axios'
import router from '../router/index'

// 创建 axios 实例
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

// 响应拦截器：处理错误
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status

    if (status === 401) {
      // 未授权，清除 token 并重定向到登录页
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      router.push('/login')
    } else if (status === 400) {
      // 表单验证错误 → 不跳转，交给各页面自行处理
      // do nothing, just reject
    } else if (status === 403 || status >= 500 || !error.response) {
      // 登录/注册页面的错误不拦截，交给页面自行处理
      const requestUrl = error.config?.url || ''
      const isAuthRequest = requestUrl.startsWith('/auth/')
      
      if (!isAuthRequest) {
        // 403 禁止访问 / 500+ 服务器错误 / 网络错误 → 跳转到首页
        const msg = !error.response
          ? '网络连接异常，请检查网络后重试'
          : `操作失败（错误码：${status}），已返回首页`
        
        alert(msg)

        // 避免在首页重复跳转
        if (router.currentRoute?.value?.path !== '/') {
          router.push('/')
        }
      }
    }

    return Promise.reject(error)
  }
)

export default apiClient
