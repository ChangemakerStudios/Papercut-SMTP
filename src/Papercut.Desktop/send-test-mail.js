var nodemailer = require('nodemailer');

var transporter = nodemailer.createTransport({
    host: 'localhost',
    port: 25,
    secure: false
});

var mailOptions = {
  from: 'me@here',
  to: 'you@papercut',
  subject: 'This is a test email.',
  text: 'That was easy!'
};

console.log('process.env.DEBUG_PAPERCUT', process.env.DEBUG_PAPERCUT);
return;
transporter.sendMail(mailOptions, function(error, info){
  if (error) {
    console.log(error);
  } else {
    console.log('Email sent: ' + info.response);
  }
  process.exit(0);
});