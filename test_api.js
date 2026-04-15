const http = require('http');

function request(method, path, body, headers = {}) {
  return new Promise((resolve, reject) => {
    const options = {
      hostname: 'localhost', port: 5000,
      path, method,
      headers: { 'Content-Type': 'application/json', ...headers }
    };
    const req = http.request(options, (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => resolve({ status: res.statusCode, body: data }));
    });
    req.on('error', reject);
    if (body) req.write(JSON.stringify(body));
    req.end();
  });
}

async function main() {
  console.log('=== Login ===');
  const login = await request('POST', '/api/auth/login', { userName: 'admin', password: 'admin' });
  console.log('Status:', login.status);
  if (login.status !== 200) { console.log('Body:', login.body); return; }
  
  const { token } = JSON.parse(login.body);
  console.log('Token OK');

  console.log('\n=== GET /api/contracts/today-stats ===');
  const stats = await request('GET', '/api/contracts/today-stats', null, { Authorization: `Bearer ${token}` });
  console.log('Status:', stats.status);
  console.log('Body:', stats.body.substring(0, 2000));

  console.log('\n=== GET /api/contracts (list) ===');
  const list = await request('GET', '/api/contracts', null, { Authorization: `Bearer ${token}` });
  console.log('Status:', list.status);
  console.log('Body:', list.body.substring(0, 1000));

  console.log('\n=== GET /api/notifications/unread-count-by-category ===');
  const notif = await request('GET', '/api/notifications/unread-count-by-category', null, { Authorization: `Bearer ${token}` });
  console.log('Status:', notif.status);
  console.log('Body:', notif.body.substring(0, 2000));
}

main().catch(console.error);
