<template>
  <div>
    <h2>Afiliados</h2>
    <table v-if="afiliados.length">
      <thead>
        <tr>
          <th>#</th>
          <th>Nombre</th>
          <th>DNI</th>
          <th>Email</th>
          <th>Teléfono</th>
          <th>Plan</th>
          <th>Consultas usadas</th>
          <th>Bloqueado</th>
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="a in afiliados" :key="a.id">
          <td>{{ a.id }}</td>
          <td>{{ a.nombreCompleto }}</td>
          <td>{{ a.dni }}</td>
          <td>{{ a.email }}</td>
          <td>{{ a.telefono }}</td>
          <td>{{ a.plan }}</td>
          <td :class="{ 'limite-alcanzado': a.consultasUsadas >= 6 }">
            {{ a.consultasUsadas }} / 6
          </td>
          <td>
            <span v-if="a.bloqueado" style="color: #d32f2f; font-weight: 600">Sí</span>
            <span v-else style="color: #388e3c">No</span>
          </td>
          <td>
            <div class="acciones">
              <button v-if="a.bloqueado" @click="desbloquear(a)">Desbloquear</button>
              <button class="btn-danger" @click="eliminar(a.id)">Eliminar</button>
            </div>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else>No hay afiliados registrados.</p>
  </div>
</template>

<script>
import { afiliadosApi } from '../services/api'

export default {
  name: 'AfiliadosList',
  data() {
    return {
      afiliados: []
    }
  },
  async mounted() {
    try {
      const res = await afiliadosApi.getAll()
      this.afiliados = res.data
    } catch {
      alert('Error al procesar la solicitud')
    }
  },
  methods: {
      async desbloquear(afiliado) {
      if (!confirm(`¿Desbloquear a ${afiliado.nombreCompleto}?`)) return
      try {
        await afiliadosApi.unlock(afiliado.id)
        alert(`${afiliado.nombreCompleto} fue desbloqueado.`)
        await this.cargarAfiliados()
      } catch (err) {
        alert(mensajeError(err))
      }
    },
    async eliminar(id) {
      try {
        await afiliadosApi.delete(id)
        this.afiliados = this.afiliados.filter(a => a.id !== id)
      } catch {
        alert('Error al procesar la solicitud')
      }
    }
  }
  
}
</script>
<style scoped>

.limite-alcanzado {
  color: #b3261e;
  font-weight: 600;
}
.acciones {
  display: flex;
  gap: 8px;
  white-space: nowrap;
}
</style>