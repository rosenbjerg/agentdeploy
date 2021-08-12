FROM debian:stable-slim
USER root
RUN apt update && apt install -y openssh-server
RUN su -c 'apt install -y sudo'
RUN echo "root:root" | chpasswd

RUN adduser --gecos "" --disabled-password testuser
RUN usermod -aG sudo testuser
RUN echo "testuser:testuser" | chpasswd
RUN printf "testuser ALL=(ALL) NOPASSWD: ALL" >> /etc/sudoers

RUN sed -i '/PermitRootLogin/c\PermitRootLogin yes' /etc/ssh/sshd_config && \
    sed -i '/PubkeyAuthentication/c\PubkeyAuthentication yes' /etc/ssh/sshd_config && \
    sed -i '/PasswordAuthentication/c\PasswordAuthentication yes' /etc/ssh/sshd_config && \
    sed -i '/ChallengeResponseAuthentication/c\ChallengeResponseAuthentication yes' /etc/ssh/sshd_config

COPY id_rsa.pub /tmp/publickey
RUN mkdir -p /root/.ssh && cat /tmp/publickey > /root/.ssh/authorized_keys && chown root /root/.ssh -R  && chmod -R go= /root/.ssh
RUN mkdir -p /home/testuser/.ssh && cat /tmp/publickey > /home/testuser/.ssh/authorized_keys && chown testuser /home/testuser/.ssh -R  && chmod -R go= /home/testuser/.ssh

EXPOSE 22 

CMD ssh-keygen -A && service ssh restart && sleep 100000