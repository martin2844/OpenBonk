# Guia de instalação do OpenBonk beta

> Tradução automática do inglês. Por favor, relate instruções pouco claras.

> OpenBonk é uma porta comunitária não oficial em testes. Cada testador precisa de sua própria cópia Steam compatível do Megabonk.

## O que você precisa

- Um dispositivo Android de 64 bits com Android 8 ou posterior. A versão beta foi testada em um Retroid Pocket 5 e um iPlay 70 mini Pro com Android 15; a compatibilidade com outros dispositivos ainda não foi estabelecida.
- Sua própria instalação Steam compatível do Megabonk em um computador. O inicializador verifica arquivos específicos suportados; uma atualização de jogo diferente pode não ser compatível.
- Espaço extra livre para ZIP, preparação e dados do jogo. Os requisitos variam de acordo com o dispositivo. A preparação pode levar vários minutos (cerca de quatro minutos em um teste do iPlay 70 mini Pro).
- O `OpenBonk.apk` no pacote beta. Faça backup de qualquer progresso de teste existente antes de substituir uma instalação. Um APK assinado de forma diferente pode exigir uma instalação separada.

## Obtenha o pacote beta

Baixe `OpenBonk-beta.zip` da [versão beta](https://github.com/martin2844/OpenBonk/releases/tag/v0.1.0-beta) e extraia-o em seu computador. Ele contém `OpenBonk.apk`, um guia de instalação e uma pasta `tools`. Mantenha toda a pasta `tools` se precisar do auxiliar ZIP. O APK é o instalador; você deve criar separadamente um ZIP privado de sua própria cópia do Steam.

## Crie o ZIP dos dados do jogo

1. No Steam, abra as propriedades do Megabonk e selecione **Arquivos Instalados → Navegar**.
2. Encontre a pasta `Megabonk_Data`. No Windows, clique com o botão direito e escolha **Enviar para → Pasta compactada (zipada)**; no Windows 11, pode estar em **Mostrar mais opções**. No Linux, use a opção de compactação do gerenciador de arquivos e selecione ZIP. Compacte toda a pasta, não apenas seu conteúdo: o ZIP deve conter uma pasta `Megabonk_Data` com suas subpastas. [Microsoft: compactar e descompactar arquivos](https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/zip-and-unzip-files).
3. Certifique-se de que o ZIP não seja maior que **1 GiB**. Conecte e desbloqueie seu dispositivo Android, escolha **Transferência de arquivos** na notificação USB e copie o Steam ZIP e o `OpenBonk.apk` da versão beta para Downloads. Mantenha o Steam ZIP privado: ele contém arquivos da cópia do seu jogo. [Android: transferir arquivos por USB](https://support.google.com/android/answer/9064445?hl=en).

Um ZIP completo de instalação do Linux foi testado no Retroid e no iPlay. Um ZIP menor com os 39 arquivos suportados do Windows também foi importado no iPlay e chegou ao Forest. A preparação do ZIP em um PC físico com Windows e a importação de uma pasta completa de instalação do Windows ainda não foram verificadas.

Se o ZIP completo exceder 1 GiB, a pasta `tools` do pacote contém um auxiliar que valida os 39 arquivos necessários e cria um ZIP menor. No Windows, abra o PowerShell na pasta que contém `tools` e execute este comando, substituindo o primeiro caminho pela pasta do seu jogo:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\create-launcher-steam-zip.ps1 "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

No Linux, da mesma pasta e com Python 3.9 ou posterior:

```sh
python3 tools/create-launcher-steam-zip.py "/ruta/a/Megabonk/Megabonk_Data" "$HOME/OpenBonk-Steam.zip"
```

No Windows, se o Python 3.9 ou posterior estiver instalado, você também poderá executar o auxiliar Python do PowerShell:

```powershell
py -3 .\tools\create-launcher-steam-zip.py "C:\ruta\a\Megabonk\Megabonk_Data" "$env:USERPROFILE\Desktop\OpenBonk-Steam.zip"
```

Copie o ZIP do auxiliar para Downloads no Android e selecione-o em vez do ZIP completo. Os auxiliares não sobrescrevem um ZIP existente e rejeitam versões de arquivos não reconhecidas. O auxiliar Python foi executado usando Windows Python no Wine e seu ZIP foi importado no iPlay. O auxiliar do PowerShell foi testado com o PowerShell 7 no Linux. Nenhum dos auxiliares foi testado em um PC nativo com Windows.

## Instale e jogue

1. Abra `OpenBonk.apk` no gerenciador de arquivos do Android. Se você vir detalhes do arquivo, procure **Abrir com → Instalador de pacote**. Se o Android solicitar permissão para instalar esse aplicativo, leia a solicitação e siga as instruções. Você pode desativar essa permissão posteriormente.
2. Abra o OpenBonk. No inicializador, escolha **Controller** ou **Touch**.
3. Selecione **Import Steam game**, encontre o ZIP em Downloads e selecione-o. **Import prepared Android data** é para um pacote Android previamente preparado, não para o Steam ZIP. Mantenha o aplicativo aberto e aguarde **Data ready**. A mensagem muda entre os estágios, mas nenhuma porcentagem é mostrada.
4. Selecione **Play**. Na primeira inicialização, textos e texturas são importados antes que o menu do jogo apareça.

## Importe e faça backup do progresso

A importação de um perfil Steam é opcional. Primeiro feche o jogo no seu computador e mantenha um backup. Encontre a pasta da conta em `~/.config/unity3d/Ved/Megabonk/Saves/CloudDir/` no Linux ou tente `%USERPROFILE%\AppData\LocalLow\Ved\Megabonk\Saves\CloudDir\` no Windows (o caminho do Windows não foi verificado). ZIP `progression.json` e `stats.json` da mesma conta juntos, copie o ZIP para Android e escolha **Import profile ZIP**. Iniciar o jogo substitui o progresso da conta Android atual, portanto, mantenha seu backup original. Esta é uma importação única, não uma sincronização de PC para Android. O perfil ZIP é privado; não o anexe a um relatório.

Para fazer backup do progresso do Android, feche o jogo completamente, abra o inicializador e selecione **Export progress**. Escolha um perfil e salve o ZIP em algum lugar privado. Você pode restaurá-lo usando **Import profile ZIP**. O ZIP contém progresso e estatísticas, mas não controles ou mods. A exportação foi testada em um iPlay com perfil de teste; mantenha o backup original do Steam também.

**Backups do Android:** esta versão não desativa o Backup automático. Dependendo das configurações do dispositivo, o Android pode incluir dados de aplicativos, como salvamentos ou importação de perfil pendente, em backups de contas e transferências de dispositivos. O APK não solicita permissão de Internet, mas o Android pode gerenciar seus próprios backups. Revise as configurações de backup do Android se não desejar isso. A desinstalação do aplicativo pode excluir dados locais; faça backup do progresso primeiro. [Backup automático do Android](https://developer.android.com/identity/data/autobackup).

## Solução de problemas

- **ZIP incompatível:** selecione `Megabonk_Data` em uma instalação completa e uma versão de jogo compatível. As atualizações do Steam podem alterar os arquivos verificados pelo inicializador.
- **Espaço insuficiente:** libere espaço e tente novamente. O lançador ainda não calcula antecipadamente os requisitos totais de espaço.
- **A importação de texto falha na inicialização:** reabra o jogo para tentar novamente enquanto o ZIP pendente permanece em cache. Se o Android limpou o cache, selecione o ZIP novamente.
- **O APK não será instalado sobre outra versão:** sua assinatura ou ID do aplicativo pode ser diferente. Faça backup de qualquer progresso que você deseja manter antes de desinstalar um aplicativo existente.
- **O Play Protect pede para verificar o APK:** em um iPlay, o Android se ofereceu para enviar o arquivo ao Google para análise antes da instalação. Leia o prompt e decida se deseja enviá-lo. O resultado dessa verificação e uma instalação completa através do instalador do sistema ainda não foram verificados.

## Informar um problema

Inclua o modelo do dispositivo, a versão do Android, se você usou um controlador ou touch, as etapas que você executou, o que esperava e o que aconteceu. Anexe uma captura de tela do erro, se for útil. Não inclua seu ZIP Steam, perfil ou informações da conta em um relatório comum.
