package com.starkaid.starkaidapp.screens

import android.util.Log
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.livedata.observeAsState
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.starkaid.starkaidapp.models.ComandoSocial
import com.starkaid.starkaidapp.viewmodels.ComandosSociaisViewModel
import com.starkaid.starkaidapp.viewmodels.ComandosSociaisViewModelFactory

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ComandosSociaisScreen(
    viewModel: ComandosSociaisViewModel = viewModel(
        factory = ComandosSociaisViewModelFactory(LocalContext.current)
    ), onBackPressed: () -> Unit = {}
) {
    Log.d("ComandosSociais", "Screen composable")

    val comandos by viewModel.comandos.observeAsState(emptyList())
    val isLoading by viewModel.isLoading.observeAsState(false)
    val errorMessage by viewModel.errorMessage.observeAsState()



    // Estados para os diálogos
    var showAddDialog by remember { mutableStateOf(false) }
    var showEditDialog by remember { mutableStateOf(false) }
    var selectedComando by remember { mutableStateOf<ComandoSocial?>(null) }

    // Estados para o formulário
    var novoComando by remember { mutableStateOf("") }
    var novaResposta by remember { mutableStateOf("") }

    LaunchedEffect(Unit) {
        viewModel.carregarComandos()
    }

    Scaffold(
        topBar = {
            TopAppBar(title = { Text("") },
                navigationIcon = {
                    IconButton(onClick = onBackPressed) {  // Usar o callback aqui
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Voltar")
                    }
                },
                actions = {
                    IconButton(onClick = { viewModel.carregarComandos() }) {
                        Icon(Icons.Default.Refresh, contentDescription = "Sincronizar")
                    }
                })
        },
        floatingActionButton = {
            FloatingActionButton(onClick = { showAddDialog = true }) {
                Icon(Icons.Filled.Add, contentDescription = "Adicionar")
            }
        }
    ) { paddingValues ->
        Box(modifier = Modifier.padding(paddingValues)) {
            if (isLoading) {
                CircularProgressIndicator(Modifier.align(Alignment.Center))
            } else {

                LazyColumn {
                    items(comandos) { comando ->
                        ComandoItem(
                            comando = comando,
                            onItemClick = {
                                selectedComando = comando
                                showEditDialog = true
                            }
                        )
                    }
                }
            }

            errorMessage?.let { message ->
                Text(
                    text = message,
                    color = MaterialTheme.colorScheme.error,
                    modifier = Modifier.padding(16.dp)
                )
            }

            // Diálogo para adicionar
            if (showAddDialog) {
                AlertDialog(
                    onDismissRequest = { showAddDialog = false },
                    title = { Text("Novo Comando") },
                    text = {
                        Column {
                            TextField(
                                value = novoComando,
                                onValueChange = { novoComando = it },
                                label = { Text("Comando") },
                                modifier = Modifier.fillMaxWidth()
                            )
                            Spacer(modifier = Modifier.height(8.dp))
                            TextField(
                                value = novaResposta,
                                onValueChange = { novaResposta = it },
                                label = { Text("Resposta") },
                                modifier = Modifier.fillMaxWidth()
                            )
                        }
                    },
                    confirmButton = {
                        Button(
                            onClick = {
                                viewModel.criarComando(
                                    comando = novoComando,
                                    resposta = novaResposta,
                                    onSuccess = {
                                        showAddDialog = false
                                        novoComando = ""
                                        novaResposta = ""
                                    }
                                )
                            }
                        ) {
                            Text("Salvar")
                        }
                    }
                )
            }

            // Diálogo para editar/excluir
            selectedComando?.let { comando ->
                if (showEditDialog) {
                    var comandoEdit by remember { mutableStateOf(comando.comando) }
                    var respostaEdit by remember { mutableStateOf(comando.resposta) }

                    AlertDialog(
                        onDismissRequest = { showEditDialog = false },
                        title = { Text("Editar Comando") },
                        text = {
                            Column {
                                TextField(
                                    value = comandoEdit,
                                    onValueChange = { comandoEdit = it },
                                    label = { Text("Comando") },
                                    modifier = Modifier.fillMaxWidth()
                                )
                                Spacer(modifier = Modifier.height(8.dp))
                                TextField(
                                    value = respostaEdit,
                                    onValueChange = { respostaEdit = it },
                                    label = { Text("Resposta") },
                                    modifier = Modifier.fillMaxWidth()
                                )
                            }
                        },
                        confirmButton = {
                            Button(
                                onClick = {
                                    viewModel.excluirComando(comando.id) {
                                        showEditDialog = false
                                    }
                                }
                            ) {
                                Text("Excluir")
                            }

                            Button(
                                onClick = {
                                    viewModel.atualizarComando(
                                        ComandoSocial(
                                            id = comando.id,
                                            userId = comando.userId,
                                            comando = comandoEdit,
                                            resposta = respostaEdit,
                                            respostasAleatorias = comando.respostasAleatorias // mantém o que já tinha
                                        ),
                                        onSuccess = {
                                            showEditDialog = false
                                        }
                                    )
                                }
                            ) {
                                Text("Salvar")
                            }
                        }
                    )
                }
            }
        }
    }
}

@Composable
fun ComandoItem(comando: ComandoSocial, onItemClick: () -> Unit) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(8.dp)
            .clickable(onClick = onItemClick),
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(text = "Comando: ${comando.comando}", style = MaterialTheme.typography.titleMedium)
            Text(text = "Resposta: ${comando.resposta}", style = MaterialTheme.typography.bodyMedium)
        }
    }
}