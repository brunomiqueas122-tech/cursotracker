// Substitui o service worker do app antigo: apaga o cache e se desregistra, para o endereço servir sempre a página de mudança.
self.addEventListener('install', () => self.skipWaiting())
self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys()
      .then((keys) => Promise.all(keys.map((k) => caches.delete(k))))
      .then(() => self.registration.unregister()),
  )
})
