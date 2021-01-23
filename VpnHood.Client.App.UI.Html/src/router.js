import Vue from 'vue';
import VueRouter from 'vue-router';
import Home from './pages/Home.vue';
import Servers from './pages/Servers.vue';

Vue.use(VueRouter);

export default new VueRouter({
  mode: 'history',
  base: process.env.BASE_URL,
  routes: [
    {
      path: '/',
      redirect: '/home'
    },
    {
      path: '/home',
      component: Home
    },
    {
      path: '/servers',
      component: Servers
    },
    {
      path: '*',
      redirect: '/',
    }
  ]
});

