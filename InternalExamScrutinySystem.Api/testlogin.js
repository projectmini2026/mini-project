fetch('http://localhost:5096/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    email: 'hod@college.edu',
    password: 'Password123!'
  })
}).then(async res => {
  console.log('Status: ', res.status);
  console.log('Body: ', await res.text());
}).catch(console.error);
