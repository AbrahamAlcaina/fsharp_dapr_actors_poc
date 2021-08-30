#!/bin/bash
# dapr
wget -q https://raw.githubusercontent.com/dapr/cli/master/install/install.sh -O - | DAPR_INSTALL_DIR="$HOME/dapr" /bin/bash
# helm 3
curl https://raw.githubusercontent.com/helm/helm/master/scripts/get-helm-3 | bash
